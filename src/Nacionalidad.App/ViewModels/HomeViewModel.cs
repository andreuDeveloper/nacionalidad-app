using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamEngine.Core.Progress;
using ExamEngine.Core.Statistics;
using Nacionalidad.App.Services;

namespace Nacionalidad.App.ViewModels;

public sealed partial class HomeViewModel(ContentService content, ProgressTracker tracker, IProgressStore store) : BaseViewModel
{
    [ObservableProperty]
    public partial string ReadinessText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Readiness { get; set; }

    [ObservableProperty]
    public partial bool HasProgress { get; set; }

    public async Task RefreshAsync()
    {
        try
        {
            var bank = await content.GetCcseAsync();
            var stats = StatisticsCalculator.Compute(bank,
                await store.GetAnswersAsync(bank.Id), await store.GetSessionsAsync(bank.Id),
                tracker, DateOnly.FromDateTime(DateTime.Today));
            HasProgress = stats.Attempts > 0;
            Readiness = stats.Readiness;
            ReadinessText = T.Format("Home_Readiness", BankText.Percent(stats.Readiness));
        }
        catch (Exception)
        {
            HasProgress = false;
        }
    }

    [RelayCommand]
    private Task OpenCcse() => Shell.Current.GoToAsync(Routes.Ccse);

    [RelayCommand]
    private Task OpenDele() => Shell.Current.GoToAsync(Routes.Dele);
}
