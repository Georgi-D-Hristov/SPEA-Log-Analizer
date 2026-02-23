using SpeaLogAnalyzer.ViewModels;

namespace SpeaLogAnalyzer.Views;

public partial class FailureAnalysisPage : ContentPage
{
    public FailureAnalysisPage(FailureAnalysisViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
