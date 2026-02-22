namespace SpeaLogAnalyzer;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(Views.SessionDetailPage), typeof(Views.SessionDetailPage));
	}
}
