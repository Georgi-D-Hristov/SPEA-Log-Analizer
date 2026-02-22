using SpeaLogAnalyzer.ViewModels;

namespace SpeaLogAnalyzer.Views;

public partial class SessionDetailPage : ContentPage
{
    public SessionDetailPage(SessionDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
