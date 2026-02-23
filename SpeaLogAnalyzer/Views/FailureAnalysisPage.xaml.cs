using SpeaLogAnalyzer.ViewModels;

namespace SpeaLogAnalyzer.Views;

public partial class FailureAnalysisPage : ContentPage
{
    private readonly FailureAnalysisViewModel _vm;

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
}
