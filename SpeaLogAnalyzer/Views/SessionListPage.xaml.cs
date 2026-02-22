using SpeaLogAnalyzer.ViewModels;

namespace SpeaLogAnalyzer.Views;

public partial class SessionListPage : ContentPage
{
    private readonly SessionListViewModel _vm;

    public SessionListPage(SessionListViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.RefreshCommand.Execute(null);
    }
}
