using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamEngine.Core.Models;
using ExamEngine.Core.Progress;
using ExamEngine.Core.Sessions;
using ExamEngine.Core.Statistics;
using Nacionalidad.App.Services;

namespace Nacionalidad.App.ViewModels;

public sealed class CategoryItem
{
    public required string Id { get; init; }
    public required string Header { get; init; }
    public required string Title { get; init; }
    public required string ProgressText { get; init; }
    public required double Coverage { get; init; }
}

public sealed partial class CcseViewModel(
    ContentService content,
    ProgressTracker tracker,
    IProgressStore store,
    SessionHolder holder,
    LanguageService language) : BaseViewModel
{
    private QuestionBank? _bank;
    private IReadOnlyList<string> _pending = [];
    private HashSet<string> _seen = [];

    public ObservableCollection<CategoryItem> Categories { get; } = [];

    [ObservableProperty]
    public partial string MockDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReviewDescription { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartReviewCommand))]
    public partial bool HasPending { get; set; }

    public async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            _bank = await content.GetCcseAsync();
            var answers = await store.GetAnswersAsync(_bank.Id);
            var stats = StatisticsCalculator.Compute(_bank, answers, await store.GetSessionsAsync(_bank.Id),
                tracker, DateOnly.FromDateTime(DateTime.Today));
            _pending = tracker.PendingReview((await tracker.GetStatesAsync(_bank.Id)).Values)
                .Where(id => _bank.FindQuestion(id) is not null).ToList();
            _seen = answers.Select(a => a.QuestionId).ToHashSet();

            var exam = _bank.FindExam(ContentService.CcseExamId)!;
            MockDescription = T.Format("Ccse_MockDesc", exam.TotalQuestions, exam.TimeLimitMinutes, exam.PassMark);
            HasPending = _pending.Count > 0;
            ReviewDescription = HasPending ? T.Format("Ccse_ReviewDesc", _pending.Count) : T["Ccse_ReviewEmpty"];

            Categories.Clear();
            foreach (var c in stats.Categories)
            {
                Categories.Add(new CategoryItem
                {
                    Id = c.CategoryId,
                    Header = T.Format("Ccse_Task", BankText.CategoryNumber(_bank, c.CategoryId)),
                    Title = BankText.CategoryTitle(_bank, c.CategoryId, language.Code),
                    ProgressText = T.Format("Ccse_TaskProgress", c.Seen, c.TotalQuestions, BankText.Percent(c.Accuracy)),
                    Coverage = c.Coverage,
                });
            }
        }
        catch (Exception)
        {
            await AlertAsync(T["Common_Error"], T["Error_LoadBank"]);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task StartMock() => Start(f => f.CreateMockExam(ContentService.CcseExamId));

    [RelayCommand(CanExecute = nameof(HasPending))]
    private Task StartReview() => Start(f => f.CreateReview(_pending, maxQuestions: 25));

    /// <summary>Práctica de una tarea: primero las preguntas nunca vistas.</summary>
    [RelayCommand]
    private Task StartPractice(CategoryItem item) =>
        Start(f => f.CreatePractice(item.Id, prioritize: UnseenIn(item.Id)));

    [RelayCommand]
    private Task StartAll() => Start(f => f.CreatePractice(null, maxQuestions: 25, prioritize: UnseenIn(null)));

    private HashSet<string> UnseenIn(string? categoryId) =>
        (_bank?.Questions ?? [])
            .Where(q => (categoryId is null || q.CategoryId == categoryId) && !_seen.Contains(q.Id))
            .Select(q => q.Id).ToHashSet();

    private async Task Start(Func<SessionFactory, ExamSession> create)
    {
        if (_bank is null) return;
        var session = create(new SessionFactory(_bank));
        holder.Start(_bank, session);
        await Shell.Current.GoToAsync(Routes.Quiz);
    }
}
