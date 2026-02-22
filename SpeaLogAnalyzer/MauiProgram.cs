using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using LiveChartsCore.SkiaSharpView.Maui;
using SkiaSharp.Views.Maui.Controls.Hosting;
using SpeaLogAnalyzer.Services;
using SpeaLogAnalyzer.Services.Interfaces;
using SpeaLogAnalyzer.ViewModels;
using SpeaLogAnalyzer.Views;

namespace SpeaLogAnalyzer;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseSkiaSharp()
			.UseLiveCharts()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Services
		builder.Services.AddSingleton<ILogParserService, LogParserService>();
		builder.Services.AddSingleton<IStatisticsService, StatisticsService>();

		// ViewModels
		builder.Services.AddSingleton<DashboardViewModel>();
		builder.Services.AddSingleton<SessionListViewModel>();
		builder.Services.AddTransient<SessionDetailViewModel>();

		// Views
		builder.Services.AddSingleton<DashboardPage>();
		builder.Services.AddSingleton<SessionListPage>();
		builder.Services.AddTransient<SessionDetailPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
