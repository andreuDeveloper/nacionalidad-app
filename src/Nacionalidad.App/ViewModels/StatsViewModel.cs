using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamEngine.Core.Progress;
using ExamEngine.Core.Statistics;
using Nacionalidad.App.Services;

namespace Nacionalidad.App.ViewModels;

public sealed class StatTile
{
    public required string Label { get; init; }
    public required string Value { get; init; }
}

public sealed partial class StatsViewModel(
    ContentService content,
    ProgressTracker tracker,
    IProgressStore store,
    LanguageService language) : BaseViewModel
{
    public ObservableCollection<StatTile> Tiles { get; } = [];
    public ObservableCollection<CategoryItem> Categories { get; } = [];

    [ObservableProperty] public partial bool HasData { get; set; }
    [ObservableProperty] public partial double Readiness { get; set; }
    [ObservableProperty] public partial string ReadinessText { get; set; } = string.Empty;

    public async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            var bank = await content.GetCcseAsync();
            var s = StatisticsCalculator.Compute(bank,
                await store.GetAnswersAsync(bank.Id), await store.GetSessionsAsync(bank.Id),
                tracker, DateOnly.FromDateTime(DateTime.Today));

            HasData = s.Attempts > 0 || s.MockExamsTaken > 0;
            Readiness = s.Readiness;
            ReadinessText = BankText.Percent(s.Readiness);

            Tiles.Clear();
            Tiles.Add(new StatTile { Label = T["Stats_Seen"], Value = $"{s.Seen}/{s.TotalQuestions}" });
            Tiles.Add(new StatTile { Label = T["Stats_Mastered"], Value = $"{s.Mastered}" });
            Tiles.Add(new StatTile { Label = T["Stats_Accuracy"], Value = BankText.Percent(s.Accuracy) });
            Tiles.Add(new StatTile { Label = T["Stats_Pending"], Value = $"{s.PendingReview}" });
            Tiles.Add(new StatTile
            {
                Label = T["Stats_Mocks"],
                Value = T.Format("Stats_MocksPassed", s.MockExamsPassed, s.MockExamsTaken)
                        + (s.LastMockExamRatio is { } last ? "\n" + T.Format("Stats_LastMock", BankText.Percent(last)) : string.Empty),
            });
            Tiles.Add(new StatTile { Label = T["Stats_Streak"], Value = T.Format("Stats_StreakDays", s.StudyStreakDays) });

            Categories.Clear();
            foreach (var c in s.Categories)
            {
                Categories.Add(new CategoryItem
                {
                    Id = c.CategoryId,
                    Header = T.Format("Ccse_Task", BankText.CategoryNumber(bank, c.CategoryId)),
                    Title = BankText.CategoryTitle(bank, c.CategoryId, language.Code),
                    ProgressText = T.Format("Ccse_TaskProgress", c.Seen, c.TotalQuestions, BankText.Percent(c.Accuracy)),
                    Coverage = c.MasteryRatio,
                });
            }
        }
        catch (Exception)
        {
            HasData = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Reset()
    {
        if (!await ConfirmAsync(T["Stats_Reset"], T["Stats_ResetConfirm"])) return;
        var bank = await content.GetCcseAsync();
        await store.ResetAsync(bank.Id);
        await RefreshAsync();
    }
}
