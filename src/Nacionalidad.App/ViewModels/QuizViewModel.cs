using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamEngine.Core.Models;
using ExamEngine.Core.Progress;
using ExamEngine.Core.Sessions;
using Nacionalidad.App.Services;

namespace Nacionalidad.App.ViewModels;

public enum OptionState
{
    Normal,
    Selected,
    Correct,
    Wrong,
}

public sealed partial class OptionItem : ObservableObject
{
    public required string Id { get; init; }
    public required string Letter { get; init; }
    public required string Text { get; init; }

    [ObservableProperty]
    public partial OptionState State { get; set; }
}

/// <summary>
/// Pantalla de preguntas. Práctica y repaso: corrección inmediata con explicación.
/// Simulacro: cronómetro, se puede cambiar la respuesta y se corrige al entregar.
/// </summary>
public sealed partial class QuizViewModel(
    SessionHolder holder,
    ProgressTracker tracker,
    LanguageService language,
    IDispatcher dispatcher) : BaseViewModel
{
    private ExamSession? _session;
    private QuestionBank? _bank;
    private IDispatcherTimer? _timer;
    private bool _finishing;

    public ObservableCollection<OptionItem> Options { get; } = [];

    [ObservableProperty] public partial int Index { get; set; }
    [ObservableProperty] public partial string ProgressText { get; set; } = string.Empty;
    [ObservableProperty] public partial double Progress { get; set; }
    [ObservableProperty] public partial string CategoryText { get; set; } = string.Empty;
    [ObservableProperty] public partial string QuestionText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsMock { get; set; }
    [ObservableProperty] public partial string TimeLeftText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsTimeLow { get; set; }
    [ObservableProperty] public partial string AnsweredText { get; set; } = string.Empty;

    [ObservableProperty] public partial bool ShowFeedback { get; set; }
    [ObservableProperty] public partial bool FeedbackIsCorrect { get; set; }
    [ObservableProperty] public partial string FeedbackTitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string CorrectAnswerText { get; set; } = string.Empty;
    [ObservableProperty] public partial string ExplanationText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool HasExplanation { get; set; }
    [ObservableProperty] public partial FlowDirection ExplanationDirection { get; set; }
    [ObservableProperty] public partial bool ExplanationNotTranslated { get; set; }

    [ObservableProperty] public partial bool CanGoPrevious { get; set; }
    [ObservableProperty] public partial bool CanGoNext { get; set; }
    [ObservableProperty] public partial bool IsLast { get; set; }

    public void Start()
    {
        _session = holder.Session;
        _bank = holder.Bank;
        if (_session is null || _bank is null) return;

        _finishing = false;
        IsMock = _session.Mode == SessionMode.MockExam;
        Index = 0;
        ShowQuestion();

        if (_session.TimeLimit is not null)
        {
            _timer?.Stop();
            _timer = dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTick;
            _timer.Start();
            UpdateTimer();
        }
    }

    public void Stop() => _timer?.Stop();

    private SessionItem Current => _session!.Items[Index];

    private void ShowQuestion()
    {
        if (_session is null || _bank is null) return;
        var item = Current;
        var q = item.Question;

        ProgressText = T.Format("Quiz_Progress", Index + 1, _session.Count);
        Progress = (Index + 1.0) / _session.Count;
        CategoryText = $"{T.Format("Ccse_Task", BankText.CategoryNumber(_bank, q.CategoryId))} · {BankText.CategoryTitle(_bank, q.CategoryId, language.Code)}";
        // El enunciado y las opciones se muestran siempre en el idioma del examen.
        QuestionText = q.Prompt.Get(_bank.DefaultLanguage);

        Options.Clear();
        for (var i = 0; i < item.Options.Count; i++)
        {
            var o = item.Options[i];
            Options.Add(new OptionItem
            {
                Id = o.Id,
                Letter = ((char)('a' + i)).ToString(),
                Text = o.Text.Get(_bank.DefaultLanguage),
            });
        }

        UpdateOptionStates();
        UpdateNavigation();
    }

    private void UpdateOptionStates()
    {
        var item = Current;
        var reveal = !IsMock && item.IsAnswered;
        foreach (var o in Options)
        {
            o.State = reveal
                ? o.Id == item.Question.CorrectOptionId ? OptionState.Correct
                  : o.Id == item.SelectedOptionId ? OptionState.Wrong
                  : OptionState.Normal
                : o.Id == item.SelectedOptionId ? OptionState.Selected : OptionState.Normal;
        }

        ShowFeedback = reveal;
        if (reveal) FillFeedback(item);
    }

    private void FillFeedback(SessionItem item)
    {
        var q = item.Question;
        FeedbackIsCorrect = item.IsCorrect;
        FeedbackTitle = item.IsCorrect ? T["Quiz_Correct"] : T["Quiz_Incorrect"];
        var correct = Options.First(o => o.Id == q.CorrectOptionId);
        CorrectAnswerText = T.Format("Quiz_CorrectAnswerIs", $"{correct.Letter}) {correct.Text}");

        HasExplanation = q.Explanation is { IsEmpty: false };
        if (HasExplanation)
        {
            var translated = q.Explanation!.Has(language.Code);
            var shownIn = translated ? language.Code : _bank!.DefaultLanguage;
            ExplanationText = q.Explanation.Get(shownIn, _bank!.DefaultLanguage);
            ExplanationDirection = LanguageService.DirectionFor(shownIn);
            ExplanationNotTranslated = !translated && language.Code != _bank.DefaultLanguage;
        }
    }

    private void UpdateNavigation()
    {
        var item = Current;
        IsLast = Index == _session!.Count - 1;
        CanGoPrevious = IsMock && Index > 0;
        // En práctica hay que responder antes de avanzar; en simulacro se puede saltar.
        CanGoNext = IsMock ? !IsLast : item.IsAnswered;
        AnsweredText = T.Format("Quiz_Answered", _session.AnsweredCount, _session.Count);
    }

    [RelayCommand]
    private async Task Select(OptionItem option)
    {
        if (_session is null || _finishing) return;
        if (!_session.Answer(Index, option.Id)) return;

        UpdateOptionStates();
        UpdateNavigation();

        if (!IsMock)
        {
            await tracker.RecordAnswerAsync(_session.BankId, Current, _session.Mode);
            if (IsLast) CanGoNext = true; // "Terminar"
        }
    }

    [RelayCommand]
    private async Task Next()
    {
        if (_session is null) return;
        if (IsLast)
        {
            if (IsMock) await Submit();
            else await FinishAsync();
            return;
        }
        Index++;
        ShowQuestion();
    }

    [RelayCommand]
    private void Previous()
    {
        if (_session is null || Index == 0) return;
        Index--;
        ShowQuestion();
    }

    [RelayCommand]
    private async Task Submit()
    {
        if (_session is null) return;
        var ok = await ConfirmAsync(T["Quiz_ConfirmSubmitTitle"],
            T.Format("Quiz_ConfirmSubmitText", _session.AnsweredCount, _session.Count));
        if (ok) await FinishAsync();
    }

    /// <summary>Botón atrás. Devuelve true si se puede salir.</summary>
    public async Task<bool> ConfirmExitAsync()
    {
        if (_session is null || _session.IsFinished) return true;
        if (!IsMock)
        {
            if (_session.AnsweredCount == 0) return true;
            if (await ConfirmAsync(T["Quiz_ConfirmExitTitle"], T["Quiz_ConfirmExitPractice"]))
            {
                await FinishAsync();
            }
            return false;
        }
        return await ConfirmAsync(T["Quiz_ConfirmExitTitle"], T["Quiz_ConfirmExitMock"]);
    }

    [RelayCommand]
    private async Task Exit()
    {
        if (await ConfirmExitAsync())
        {
            Stop();
            holder.Clear();
            await Shell.Current.GoToAsync("..");
        }
    }

    private async Task FinishAsync()
    {
        if (_session is null || _finishing) return;
        _finishing = true;
        Stop();

        var result = _session.Finish();
        // En práctica/repaso las respuestas ya se guardaron una a una.
        await tracker.RecordAsync(result, includeAnswers: _session.Mode == SessionMode.MockExam);
        holder.Complete(result);
        // Sustituye la pantalla de preguntas por la de resultado.
        await Shell.Current.GoToAsync($"../{Routes.Result}");
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        UpdateTimer();
        if (_session is { IsTimeUp: true } && !_finishing)
        {
            Stop();
            await AlertAsync(T["Quiz_TimeUpTitle"], T["Quiz_TimeUpText"]);
            await FinishAsync();
        }
    }

    private void UpdateTimer()
    {
        if (_session?.Remaining is not { } r) return;
        TimeLeftText = BankText.Duration(r);
        IsTimeLow = r < TimeSpan.FromMinutes(5);
    }
}
