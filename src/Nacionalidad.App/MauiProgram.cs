using CommunityToolkit.Maui;
using ExamEngine.Core.Progress;
using Microsoft.Extensions.Logging;
using Nacionalidad.App.Data;
using Nacionalidad.App.Services;
using Nacionalidad.App.ViewModels;
using Nacionalidad.App.Views;

namespace Nacionalidad.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var s = builder.Services;

        // Plataforma
        s.AddSingleton(Preferences.Default);
        s.AddSingleton(AppInfo.Current);
        s.AddSingleton(sp => Application.Current?.Dispatcher ?? Dispatcher.GetForCurrentThread()!);

        // Datos y motor
        s.AddSingleton<AppDatabase>();
        s.AddSingleton<IProgressStore, SqliteProgressStore>();
        s.AddSingleton(sp => new ProgressTracker(sp.GetRequiredService<IProgressStore>(), correctStreakToClear: 2, correctStreakToMaster: 3));

        // Servicios
        s.AddSingleton<LanguageService>();
        s.AddSingleton<ContentService>();
        s.AddSingleton<SessionHolder>();
        s.AddSingleton<IBillingService, BillingService>();

        // Shell, páginas y viewmodels
        s.AddSingleton<AppShell>();
        s.AddTransient<HomePage>().AddTransient<HomeViewModel>();
        s.AddTransient<CcsePage>().AddTransient<CcseViewModel>();
        s.AddTransient<QuizPage>().AddTransient<QuizViewModel>();
        s.AddTransient<ResultPage>().AddTransient<ResultViewModel>();
        s.AddTransient<StatsPage>().AddTransient<StatsViewModel>();
        s.AddTransient<SettingsPage>().AddTransient<SettingsViewModel>();
        s.AddTransient<DelePage>().AddTransient<DeleViewModel>();

        return builder.Build();
    }
}
