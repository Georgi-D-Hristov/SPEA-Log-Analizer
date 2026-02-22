using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SpeaLogAnalyzer.Models;
using SpeaLogAnalyzer.Services.Interfaces;

namespace SpeaLogAnalyzer.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ILogParserService _logParser;
    private readonly IStatisticsService _statistics;

    private List<TestSession> _allSessions = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _progressPercent;

    [ObservableProperty]
    private string _statusText = "Select a folder with SPEA log files to begin.";

    [ObservableProperty]
    private string _loadedFolderPath = string.Empty;

    [ObservableProperty]
    private int _totalSessions;

    [ObservableProperty]
    private int _totalBoardsTested;

    [ObservableProperty]
    private int _passedBoardsCount;

    [ObservableProperty]
    private int _failedBoardsCount;

    [ObservableProperty]
    private double _overallYield;

    [ObservableProperty]
    private ISeries[] _yieldPieSeries = [];

    [ObservableProperty]
    private ISeries[] _paretoSeries = [];

    [ObservableProperty]
    private Axis[] _paretoXAxes = [];

    [ObservableProperty]
    private Axis[] _paretoYAxes = [];

    [ObservableProperty]
    private ISeries[] _yieldTrendSeries = [];

    [ObservableProperty]
    private Axis[] _trendXAxes = [];

    [ObservableProperty]
    private Axis[] _trendYAxes = [];

    [ObservableProperty]
    private bool _hasData;

    public DashboardViewModel(ILogParserService logParser, IStatisticsService statistics)
    {
        _logParser = logParser;
        _statistics = statistics;
    }

    public List<TestSession> AllSessions => _allSessions;

    [RelayCommand]
    private async Task LoadFolderAsync()
    {
        try
        {
            var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
            if (!result.IsSuccessful || string.IsNullOrEmpty(result.Folder?.Path))
                return;

            var folderPath = result.Folder.Path;
            LoadedFolderPath = folderPath;
            IsLoading = true;
            HasData = false;
            StatusText = "Parsing log files...";
            ProgressPercent = 0;

            var progress = new Progress<int>(p =>
            {
                ProgressPercent = p;
                StatusText = $"Parsing log files... {p}%";
            });

            _allSessions = await Task.Run(() => _logParser.ParseFolderAsync(folderPath, progress));

            UpdateStatistics();
            BuildCharts();

            HasData = _allSessions.Count > 0;
            StatusText = $"Loaded {_allSessions.Count} sessions from {Path.GetFileName(folderPath)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateStatistics()
    {
        TotalSessions = _allSessions.Count;
        TotalBoardsTested = _statistics.TotalBoards(_allSessions);
        PassedBoardsCount = _statistics.PassedBoards(_allSessions);
        FailedBoardsCount = _statistics.FailedBoards(_allSessions);
        OverallYield = _statistics.CalculateYield(_allSessions);
    }

    private void BuildCharts()
    {
        BuildPieChart();
        BuildParetoChart();
        BuildTrendChart();
    }

    private void BuildPieChart()
    {
        YieldPieSeries =
        [
            new PieSeries<double>
            {
                Values = [PassedBoardsCount],
                Name = "PASS",
                Fill = new SolidColorPaint(SKColor.Parse("#4CAF50")),
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue:N0}",
                DataLabelsPaint = new SolidColorPaint(SKColors.White)
            },
            new PieSeries<double>
            {
                Values = [FailedBoardsCount],
                Name = "FAIL",
                Fill = new SolidColorPaint(SKColor.Parse("#F44336")),
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue:N0}",
                DataLabelsPaint = new SolidColorPaint(SKColors.White)
            }
        ];
    }

    private void BuildParetoChart()
    {
        var topFailing = _statistics.GetTopFailingComponents(_allSessions, 10);
        if (topFailing.Count == 0)
        {
            ParetoSeries = [];
            return;
        }

        var labels = topFailing.Select(x => x.ComponentName).ToArray();
        var values = topFailing.Select(x => (double)x.FailCount).ToArray();

        ParetoSeries =
        [
            new ColumnSeries<double>
            {
                Values = values,
                Name = "Fail Count",
                Fill = new SolidColorPaint(SKColor.Parse("#F44336")),
                DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue:N0}",
                DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                MaxBarWidth = 35
            }
        ];

        ParetoXAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsRotation = 45,
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColors.Gray)
            }
        ];

        ParetoYAxes =
        [
            new Axis
            {
                Name = "Failures",
                TextSize = 11,
                NameTextSize = 13,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                NamePaint = new SolidColorPaint(SKColors.Gray)
            }
        ];
    }

    private void BuildTrendChart()
    {
        var trend = _statistics.GetYieldTrend(_allSessions);
        if (trend.Count == 0)
        {
            YieldTrendSeries = [];
            return;
        }

        var labels = trend.Select(t => t.Date.ToString("MM/dd")).ToArray();
        var values = trend.Select(t => t.YieldPercent).ToArray();

        YieldTrendSeries =
        [
            new LineSeries<double>
            {
                Values = values,
                Name = "Yield %",
                Stroke = new SolidColorPaint(SKColor.Parse("#512BD4"), 3),
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#512BD4"), 2),
                GeometryFill = new SolidColorPaint(SKColors.White),
                GeometrySize = 8,
                Fill = new SolidColorPaint(SKColor.Parse("#512BD4").WithAlpha(40))
            }
        ];

        TrendXAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsRotation = 45,
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColors.Gray)
            }
        ];

        TrendYAxes =
        [
            new Axis
            {
                Name = "Yield %",
                MinLimit = 0,
                MaxLimit = 100,
                TextSize = 11,
                NameTextSize = 13,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                NamePaint = new SolidColorPaint(SKColors.Gray)
            }
        ];
    }
}
