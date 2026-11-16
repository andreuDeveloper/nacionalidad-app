using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamEngine.Core.Models;
using ExamEngine.Core.Sessions;
using Nacionalidad.App.Services;

namespace Nacionalidad.App.ViewModels;

public sealed class CategoryResultItem
{
    public required string Title { get; init; }
    public required string Score { get; init; }
    public required double Ratio { get; init; }
}

public sealed class MistakeItem
{
    public required string Question { get; init; }
    public required string YourAnswer { get; init; }
    public required string CorrectAnswer { get; init; }
    public required string Explanation { get; init; }
    public required bool HasExplanation { get; init; }
    public required FlowDirection ExplanationDirection { get; init; }
}

public sealed partial class ResultViewModel(SessionHolder holder, LanguageService language) : BaseViewModel
{
    private ExamResult? _result;
    private QuestionBank? _bank;

    public ObservableCollection<CategoryResultItem> ByCategory { get; } = [];
    public ObservableCollection<MistakeItem> Mistakes { get; } = [];

    [ObservableProperty] public partial string Headline { get; set; } = string.Empty;
    [ObservableProperty] public partial bool HasVerdict { get; set; }
    [ObservableProperty] public partial bool Passed { get; set; }
    [ObservableProperty] public partial string ScoreText { get; set; } = string.Empty;
    [ObservableProperty] public partial string PassMarkText { get; set; } = string.Empty;
    [ObservableProperty] public partial string DurationText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool HasMistakes { get; set; }
    [ObservableProperty] public partial bool IsMock { get; set; }

    public void Load()
    {
        _result = holder.Result;
        _bank = holder.Bank;
        if (_result is null || _bank is null) return;

        IsMock = _result.Mode == SessionMode.MockExam;
        HasVerdict = _result.Passed is not null;
        Passed = _result.Passed == true;
        Headline = _result.Passed switch
        {
            true => T["Result_Passed"],
            false => T["Result_Failed"],
            null => T["Result_PracticeDone"],
        };
        ScoreText = T.Format("Result_Score", _result.Correct, _result.Total);
        PassMarkText = _result.PassMark is { } p ? T.Format("Result_PassMark", p) : string.Empty;
        DurationText = T.Format("Result_Duration", BankText.Duration(_result.Duration));

        ByCategory.Clear();
        foreach (var c in _result.ByCategory.OrderBy(c => BankText.CategoryNumber(_bank, c.CategoryId)))
        {
            ByCategory.Add(new CategoryResultItem
            {
                Title = $"{T.Format("Ccse_Task", BankText.CategoryNumber(_bank, c.CategoryId))} · {BankText.CategoryTitle(_bank, c.CategoryId, language.Code)}",
                Score = $"{c.Correct}/{c.Total}",
                Ratio = c.Ratio,
            });
        }

        Mistakes.Clear();
        foreach (var o in _result.Outcomes.Where(o => !o.IsCorrect))
        {
            var q = _bank.FindQuestion(o.QuestionId);
            if (q is null) continue;
            var lang = _bank.DefaultLanguage;
            var selected = o.SelectedOptionId is null ? null : q.FindOption(o.SelectedOptionId);
            var correct = q.FindOption(q.CorrectOptionId)!;
            var explLang = q.Explanation?.Has(language.Code) == true ? language.Code : lang;
            Mistakes.Add(new MistakeItem
            {
                Question = q.Prompt.Get(lang),
                YourAnswer = T.Format("Result_YourAnswer", selected?.Text.Get(lang) ?? T["Result_NoAnswer"]),
                CorrectAnswer = T.Format("Quiz_CorrectAnswerIs", correct.Text.Get(lang)),
                Explanation = q.Explanation?.Get(explLang, lang) ?? string.Empty,
                HasExplanation = q.Explanation is { IsEmpty: false },
                ExplanationDirection = LanguageService.DirectionFor(explLang),
            });
        }
        HasMistakes = Mistakes.Count > 0;
    }

    [RelayCommand]
    private async Task ReviewMistakes()
    {
        if (_result is null || _bank is null) return;
        var ids = _result.Outcomes.Where(o => !o.IsCorrect).Select(o => o.QuestionId).ToList();
        if (ids.Count == 0) return;
        holder.Start(_bank, new SessionFactory(_bank).CreateReview(ids));
        await Shell.Current.GoToAsync($"../{Routes.Quiz}");
    }

    [RelayCommand]
    private async Task Again()
    {
        if (_bank is null) return;
        holder.Start(_bank, new SessionFactory(_bank).CreateMockExam(ContentService.CcseExamId));
        await Shell.Current.GoToAsync($"../{Routes.Quiz}");
    }

    [RelayCommand]
    private async Task Close()
    {
        holder.Clear();
        await Shell.Current.GoToAsync("..");
    }
}
