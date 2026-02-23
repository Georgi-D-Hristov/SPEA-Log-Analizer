using SpeaLogAnalyzer.ViewModels;

namespace SpeaLogAnalyzer.Views;

public partial class FailureAnalysisPage : ContentPage
{
    private readonly FailureAnalysisViewModel _vm;
    private double _splitterStartWidth;
    private const double MinLeftWidth = 200;
    private const double MaxLeftWidth = 800;

    public FailureAnalysisPage(FailureAnalysisViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!_vm.HasData)
            _vm.RefreshCommand.Execute(null);
    }

    private void OnSplitterPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _splitterStartWidth = LeftColumn.Width.Value;
                break;

            case GestureStatus.Running:
                var newWidth = _splitterStartWidth + e.TotalX;
                newWidth = Math.Clamp(newWidth, MinLeftWidth, MaxLeftWidth);
                LeftColumn.Width = new GridLength(newWidth);
                break;
        }
    }

    private void OnResetHistogramZoom(object? sender, EventArgs e)
    {
        if (_vm.HistogramXAxes is not null)
            foreach (var axis in _vm.HistogramXAxes) { axis.MinLimit = null; axis.MaxLimit = null; }
        if (_vm.HistogramYAxes is not null)
            foreach (var axis in _vm.HistogramYAxes) { axis.MinLimit = null; axis.MaxLimit = null; }
    }

    private void OnResetScatterZoom(object? sender, EventArgs e)
    {
        if (_vm.ScatterXAxes is not null)
            foreach (var axis in _vm.ScatterXAxes) { axis.MinLimit = null; axis.MaxLimit = null; }
        if (_vm.ScatterYAxes is not null)
            foreach (var axis in _vm.ScatterYAxes) { axis.MinLimit = null; axis.MaxLimit = null; }
    }
}
