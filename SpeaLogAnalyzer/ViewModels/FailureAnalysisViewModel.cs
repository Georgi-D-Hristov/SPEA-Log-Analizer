using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SpeaLogAnalyzer.Models;

namespace SpeaLogAnalyzer.ViewModels;

public partial class FailureAnalysisViewModel : ObservableObject
{
    private readonly DashboardViewModel _dashboardVm;
    private List<TestMeasurement> _allMeasurements = [];

    [ObservableProperty]
    private List<FailureRow> _failureRows = [];

    [ObservableProperty]
    private FailureRow? _selectedFailure;

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private bool _hasSelection;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "Load data from Dashboard first.";

    [ObservableProperty]
    private string _selectionTitle = string.Empty;

    [ObservableProperty]
    private string _selectionStats = string.Empty;

    [ObservableProperty]
    private string _limitHistoryText = string.Empty;

    [ObservableProperty]
    private bool _hasLimitChanges;

    // Histogram
    [ObservableProperty]
    private ISeries[] _histogramSeries = [];

    [ObservableProperty]
    private Axis[] _histogramXAxes = [];

    [ObservableProperty]
    private Axis[] _histogramYAxes = [];

    // Scatter
    [ObservableProperty]
    private ISeries[] _scatterSeries = [];

    [ObservableProperty]
    private Axis[] _scatterXAxes = [];

    [ObservableProperty]
    private Axis[] _scatterYAxes = [];

    public FailureAnalysisViewModel(DashboardViewModel dashboardVm)
    {
        _dashboardVm = dashboardVm;
    }

    [RelayCommand]
    private async Task Refresh()
    {
        var sessions = _dashboardVm.AllSessions;
        if (sessions.Count == 0)
        {
            StatusText = "Load data from Dashboard first.";
            HasData = false;
            return;
        }

        IsLoading = true;
        StatusText = "Analyzing failures...";

        try
        {
            var allMeasurements = new List<TestMeasurement>();
            var grouped = await Task.Run(() =>
            {
                allMeasurements = sessions.SelectMany(s => s.Measurements).ToList();

                var result = allMeasurements
                    .Where(m => m.Result != TestResult.Pass && m.Result != TestResult.None)
                    .GroupBy(m => m.ComponentName)
                    .Select(g =>
                    {
                        var all = allMeasurements.Where(m => m.ComponentName == g.Key).ToList();
                        return new FailureRow
                        {
                            ComponentName = g.Key,
                            FailCount = g.Count(),
                            TotalCount = all.Count,
                            FailRate = all.Count > 0 ? Math.Round(g.Count() * 100.0 / all.Count, 1) : 0,
                            Category = g.First().Category.ToString(),
                            Description = g.First().Description,
                            Unit = g.First().Unit
                        };
                    })
                    .OrderByDescending(r => r.FailCount)
                    .ToList();

                for (int i = 0; i < result.Count; i++)
                    result[i].Rank = i + 1;

                return result;
            });

            _allMeasurements = allMeasurements;
            FailureRows = grouped;
            HasData = grouped.Count > 0;
            StatusText = HasData
                ? $"{grouped.Count} failing components from {sessions.Count} records"
                : "No failures found.";
            HasSelection = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedFailureChanged(FailureRow? value)
    {
        if (value is null)
        {
            HasSelection = false;
            return;
        }

        BuildChartsForComponent(value.ComponentName);
    }

    [RelayCommand]
    private void SelectFailure(FailureRow? row)
    {
        if (row is null) return;
        SelectedFailure = row;
    }

    private void BuildChartsForComponent(string componentName)
    {
        var measurements = _allMeasurements
            .Where(m => m.ComponentName == componentName)
            .OrderBy(m => m.SessionTime)
            .ThenBy(m => m.Channel)
            .ToList();

        if (measurements.Count == 0)
        {
            HasSelection = false;
            return;
        }

        var first = measurements[0];
        SelectionTitle = $"{componentName} — {first.Description}";

        var passCount = measurements.Count(m => m.Result == TestResult.Pass);
        var failCount = measurements.Count(m => m.Result != TestResult.Pass && m.Result != TestResult.None);
        var values = measurements.Select(m => m.MeasuredValue).ToList();
        var avg = values.Average();
        var min = values.Min();
        var max = values.Max();
        var stdDev = Math.Sqrt(values.Sum(v => (v - avg) * (v - avg)) / values.Count);

        SelectionStats = $"Total: {measurements.Count} | PASS: {passCount} | FAIL: {failCount} | " +
                         $"Avg: {avg:G5} | Min: {min:G5} | Max: {max:G5} | StdDev: {stdDev:G4} {first.Unit}";

        // Detect limit changes over time
        var limitSets = DetectLimitChanges(measurements);
        HasLimitChanges = limitSets.Count > 1;

        if (HasLimitChanges)
        {
            var lines = limitSets.Select(ls =>
                $"  {ls.From:yyyy-MM-dd HH:mm} → {ls.To:yyyy-MM-dd HH:mm}  |  " +
                $"Low: {(ls.HasLow ? ls.LowLimit.ToString("G5") : "—")}  |  " +
                $"High: {(ls.HasHigh ? ls.HighLimit.ToString("G5") : "—")}  |  " +
                $"{ls.Count} measurements");
            LimitHistoryText = $"⚠ Limit changes detected ({limitSets.Count} periods):\n{string.Join("\n", lines)}";
        }
        else
        {
            LimitHistoryText = string.Empty;
        }

        BuildHistogram(measurements, limitSets);
        BuildScatterPlot(measurements, limitSets);
        HasSelection = true;
    }

    private record LimitPeriod(
        double LowLimit, double HighLimit,
        bool HasLow, bool HasHigh,
        DateTime From, DateTime To, int Count);

    private static List<LimitPeriod> DetectLimitChanges(List<TestMeasurement> measurements)
    {
        var periods = new List<LimitPeriod>();
        double currentLow = measurements[0].LowLimit;
        double currentHigh = measurements[0].HighLimit;
        DateTime periodStart = measurements[0].SessionTime;
        int count = 0;

        foreach (var m in measurements)
        {
            // Tolerance: treat limits as same if within 0.1% of each other
            bool sameLow = Math.Abs(m.LowLimit - currentLow) < Math.Abs(currentLow) * 0.001 + 1e-15;
            bool sameHigh = Math.Abs(m.HighLimit - currentHigh) < Math.Abs(currentHigh) * 0.001 + 1e-15;

            if (!sameLow || !sameHigh)
            {
                periods.Add(new LimitPeriod(
                    currentLow, currentHigh,
                    currentLow > -1e9, currentHigh < 1e9,
                    periodStart, m.SessionTime, count));

                currentLow = m.LowLimit;
                currentHigh = m.HighLimit;
                periodStart = m.SessionTime;
                count = 0;
            }
            count++;
        }

        periods.Add(new LimitPeriod(
            currentLow, currentHigh,
            currentLow > -1e9, currentHigh < 1e9,
            periodStart, measurements[^1].SessionTime, count));

        return periods;
    }

    private void BuildHistogram(List<TestMeasurement> measurements, List<LimitPeriod> limitSets)
    {
        var values = measurements.Select(m => m.MeasuredValue).ToList();
        if (values.Count == 0) return;

        var min = values.Min();
        var max = values.Max();
        var range = max - min;

        if (range == 0)
        {
            HistogramSeries =
            [
                new ColumnSeries<double>
                {
                    Values = [(double)values.Count],
                    Name = "Count",
                    Fill = new SolidColorPaint(SKColor.Parse("#512BD4")),
                    MaxBarWidth = 50
                }
            ];
            HistogramXAxes = [new Axis { Labels = [$"{min:G5}"], TextSize = 11, LabelsPaint = new SolidColorPaint(SKColors.Gray) }];
            HistogramYAxes = [new Axis { Name = "Count", TextSize = 11, NameTextSize = 13, LabelsPaint = new SolidColorPaint(SKColors.Gray), NamePaint = new SolidColorPaint(SKColors.Gray), MinLimit = 0 }];
            _histogramSections = [];
            OnPropertyChanged(nameof(HistogramSections));
            return;
        }

        int binCount = Math.Max(5, (int)Math.Ceiling(1 + 3.322 * Math.Log10(values.Count)));
        binCount = Math.Min(binCount, 40);
        var binWidth = range / binCount;

        var bins = new int[binCount];
        var binLabels = new string[binCount];

        for (int i = 0; i < binCount; i++)
        {
            var lo = min + i * binWidth;
            binLabels[i] = $"{lo:G4}";
        }

        foreach (var v in values)
        {
            var idx = (int)((v - min) / binWidth);
            if (idx >= binCount) idx = binCount - 1;
            bins[idx]++;
        }

        var columnValues = bins.Select(b => (double)b).ToArray();

        HistogramSeries =
        [
            new ColumnSeries<double>
            {
                Values = columnValues,
                Name = "Count",
                Fill = new SolidColorPaint(SKColor.Parse("#512BD4")),
                MaxBarWidth = 30,
                Padding = 2,
                DataLabelsFormatter = p => p.Coordinate.PrimaryValue > 0 ? $"{p.Coordinate.PrimaryValue:N0}" : "",
                DataLabelsPaint = new SolidColorPaint(SKColors.Gray),
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
            }
        ];

        HistogramXAxes =
        [
            new Axis
            {
                Labels = binLabels,
                LabelsRotation = 45,
                TextSize = 10,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                Name = measurements[0].Unit,
                NameTextSize = 12,
                NamePaint = new SolidColorPaint(SKColors.Gray)
            }
        ];

        HistogramYAxes =
        [
            new Axis
            {
                Name = "Count",
                MinLimit = 0,
                TextSize = 11,
                NameTextSize = 13,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                NamePaint = new SolidColorPaint(SKColors.Gray)
            }
        ];

        // Limit lines on histogram — show ALL unique limits
        var histSections = new List<RectangularSection>();
        var lowColors = new[] { "#FF9800", "#E91E63", "#9C27B0", "#00BCD4" };
        var highColors = new[] { "#FF9800", "#E91E63", "#9C27B0", "#00BCD4" };
        int colorIdx = 0;

        var seenLimits = new HashSet<string>();
        foreach (var ls in limitSets)
        {
            var color = colorIdx < lowColors.Length ? lowColors[colorIdx] : "#FF9800";

            if (ls.HasLow && range > 0)
            {
                var key = $"L:{ls.LowLimit:G8}";
                if (seenLimits.Add(key))
                {
                    var lowBinPos = (ls.LowLimit - min) / binWidth;
                    var label = limitSets.Count > 1
                        ? $"Low ({ls.From:MM/dd})"
                        : "Low Limit";
                    histSections.Add(new RectangularSection
                    {
                        Xi = lowBinPos,
                        Xj = lowBinPos,
                        Stroke = new SolidColorPaint(SKColor.Parse(color), 2)
                        {
                            PathEffect = new LiveChartsCore.SkiaSharpView.Painting.Effects.DashEffect([6, 4])
                        },
                        Label = label,
                        LabelPaint = new SolidColorPaint(SKColor.Parse(color)),
                        LabelSize = 10
                    });
                }
            }

            if (ls.HasHigh && range > 0)
            {
                var key = $"H:{ls.HighLimit:G8}";
                if (seenLimits.Add(key))
                {
                    var highBinPos = (ls.HighLimit - min) / binWidth;
                    var label = limitSets.Count > 1
                        ? $"High ({ls.From:MM/dd})"
                        : "High Limit";
                    histSections.Add(new RectangularSection
                    {
                        Xi = highBinPos,
                        Xj = highBinPos,
                        Stroke = new SolidColorPaint(SKColor.Parse(color), 2)
                        {
                            PathEffect = new LiveChartsCore.SkiaSharpView.Painting.Effects.DashEffect([6, 4])
                        },
                        Label = label,
                        LabelPaint = new SolidColorPaint(SKColor.Parse(color)),
                        LabelSize = 10
                    });
                }
            }

            colorIdx++;
        }

        _histogramSections = histSections.ToArray();
        OnPropertyChanged(nameof(HistogramSections));
    }

    private void BuildScatterPlot(List<TestMeasurement> measurements, List<LimitPeriod> limitSets)
    {
        var passPoints = new List<LiveChartsCore.Defaults.ObservablePoint>();
        var failPoints = new List<LiveChartsCore.Defaults.ObservablePoint>();

        for (int i = 0; i < measurements.Count; i++)
        {
            var m = measurements[i];
            var point = new LiveChartsCore.Defaults.ObservablePoint(i, m.MeasuredValue);

            if (m.Result == TestResult.Pass)
                passPoints.Add(point);
            else
                failPoints.Add(point);
        }

        var series = new List<ISeries>();

        if (passPoints.Count > 0)
        {
            series.Add(new ScatterSeries<LiveChartsCore.Defaults.ObservablePoint>
            {
                Values = passPoints,
                Name = "PASS",
                Fill = new SolidColorPaint(SKColor.Parse("#4CAF50")),
                GeometrySize = 6
            });
        }

        if (failPoints.Count > 0)
        {
            series.Add(new ScatterSeries<LiveChartsCore.Defaults.ObservablePoint>
            {
                Values = failPoints,
                Name = "FAIL",
                Fill = new SolidColorPaint(SKColor.Parse("#F44336")),
                GeometrySize = 8
            });
        }

        ScatterSeries = series.ToArray();

        // Limit lines on scatter — show ALL unique limits with different colors
        var scatterSections = new List<RectangularSection>();
        var limitColors = new[] { "#FF9800", "#E91E63", "#9C27B0", "#00BCD4" };
        int colorIdx = 0;

        var seenLimits = new HashSet<string>();
        foreach (var ls in limitSets)
        {
            var color = colorIdx < limitColors.Length ? limitColors[colorIdx] : "#FF9800";

            if (ls.HasLow)
            {
                var key = $"L:{ls.LowLimit:G8}";
                if (seenLimits.Add(key))
                {
                    scatterSections.Add(new RectangularSection
                    {
                        Yi = ls.LowLimit,
                        Yj = ls.LowLimit,
                        Stroke = new SolidColorPaint(SKColor.Parse(color), 2)
                        {
                            PathEffect = new LiveChartsCore.SkiaSharpView.Painting.Effects.DashEffect([6, 4])
                        },
                        Label = limitSets.Count > 1 ? $"Low ({ls.From:MM/dd})" : "Low Limit",
                        LabelPaint = new SolidColorPaint(SKColor.Parse(color)),
                        LabelSize = 10
                    });
                }
            }

            if (ls.HasHigh)
            {
                var key = $"H:{ls.HighLimit:G8}";
                if (seenLimits.Add(key))
                {
                    scatterSections.Add(new RectangularSection
                    {
                        Yi = ls.HighLimit,
                        Yj = ls.HighLimit,
                        Stroke = new SolidColorPaint(SKColor.Parse(color), 2)
                        {
                            PathEffect = new LiveChartsCore.SkiaSharpView.Painting.Effects.DashEffect([6, 4])
                        },
                        Label = limitSets.Count > 1 ? $"High ({ls.From:MM/dd})" : "High Limit",
                        LabelPaint = new SolidColorPaint(SKColor.Parse(color)),
                        LabelSize = 10
                    });
                }
            }

            colorIdx++;
        }

        ScatterXAxes =
        [
            new Axis
            {
                Name = "Test #",
                TextSize = 11,
                NameTextSize = 12,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                NamePaint = new SolidColorPaint(SKColors.Gray)
            }
        ];

        ScatterYAxes =
        [
            new Axis
            {
                Name = measurements[0].Unit,
                TextSize = 11,
                NameTextSize = 13,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                NamePaint = new SolidColorPaint(SKColors.Gray)
            }
        ];

        _scatterSections = scatterSections.ToArray();
        OnPropertyChanged(nameof(ScatterSections));
    }

    private RectangularSection[] _scatterSections = [];
    public RectangularSection[] ScatterSections => _scatterSections;

    private RectangularSection[] _histogramSections = [];
    public RectangularSection[] HistogramSections => _histogramSections;
}

public class FailureRow
{
    public int Rank { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public int FailCount { get; set; }
    public int TotalCount { get; set; }
    public double FailRate { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    public string FailRateDisplay => $"{FailRate:F1}%";
}
