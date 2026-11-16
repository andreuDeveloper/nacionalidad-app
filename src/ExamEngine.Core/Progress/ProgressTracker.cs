using ExamEngine.Core.Sessions;

namespace ExamEngine.Core.Progress;

/// <summary>Estado de aprendizaje de una pregunta calculado a partir de su historial.</summary>
public sealed record QuestionState(
    string QuestionId,
    string CategoryId,
    int Attempts,
    int CorrectCount,
    int ConsecutiveCorrect,
    bool LastWasCorrect,
    DateTimeOffset LastAnsweredAt)
{
    public int WrongCount => Attempts - CorrectCount;
}

/// <summary>
/// Calcula el estado de cada pregunta y decide qué entra en el repaso de fallos.
/// Regla: una pregunta está "pendiente de repaso" si se ha fallado alguna vez y todavía no se ha
/// acertado <see cref="CorrectStreakToClear"/> veces seguidas desde el último fallo.
/// </summary>
public sealed class ProgressTracker
{
    private readonly IProgressStore _store;

    public ProgressTracker(IProgressStore store, int correctStreakToClear = 2, int correctStreakToMaster = 3)
    {
        if (correctStreakToClear < 1) throw new ArgumentOutOfRangeException(nameof(correctStreakToClear));
        _store = store;
        CorrectStreakToClear = correctStreakToClear;
        CorrectStreakToMaster = Math.Max(correctStreakToMaster, 1);
    }

    /// <summary>Aciertos seguidos necesarios para que una pregunta fallada salga del repaso.</summary>
    public int CorrectStreakToClear { get; }

    /// <summary>Aciertos seguidos para considerar una pregunta "dominada".</summary>
    public int CorrectStreakToMaster { get; }

    /// <summary>Guarda el resumen de una sesión terminada y, si <paramref name="includeAnswers"/>, sus respuestas.</summary>
    /// <param name="includeAnswers">false si las respuestas ya se guardaron una a una con <see cref="RecordAnswerAsync"/>.</param>
    public async Task RecordAsync(ExamResult result, bool includeAnswers = true, CancellationToken ct = default)
    {
        var at = result.FinishedAt;
        var answers = result.Outcomes
            .Where(o => o.IsAnswered)
            .Select(o => new AnswerRecord(result.BankId, o.QuestionId, o.CategoryId, o.IsCorrect, result.Mode, at))
            .ToList();
        if (includeAnswers && answers.Count > 0) await _store.AddAnswersAsync(answers, ct).ConfigureAwait(false);
        await _store.AddSessionAsync(SessionRecord.From(result), ct).ConfigureAwait(false);
    }

    /// <summary>Guarda una única respuesta al momento (útil en práctica, para no perder progreso si se cierra la app).</summary>
    public Task RecordAnswerAsync(string bankId, SessionItem item, SessionMode mode, CancellationToken ct = default)
    {
        if (!item.IsAnswered) return Task.CompletedTask;
        var record = new AnswerRecord(bankId, item.Question.Id, item.Question.CategoryId, item.IsCorrect, mode,
            item.AnsweredAt ?? DateTimeOffset.UtcNow);
        return _store.AddAnswersAsync([record], ct);
    }

    public async Task<IReadOnlyDictionary<string, QuestionState>> GetStatesAsync(string bankId, CancellationToken ct = default)
    {
        var answers = await _store.GetAnswersAsync(bankId, ct).ConfigureAwait(false);
        return BuildStates(answers);
    }

    public async Task<IReadOnlyList<string>> GetPendingReviewAsync(string bankId, CancellationToken ct = default)
    {
        var states = await GetStatesAsync(bankId, ct).ConfigureAwait(false);
        return PendingReview(states.Values).ToList();
    }

    public IEnumerable<string> PendingReview(IEnumerable<QuestionState> states) =>
        states.Where(s => s.WrongCount > 0 && s.ConsecutiveCorrect < CorrectStreakToClear)
              .OrderByDescending(s => s.WrongCount)
              .ThenBy(s => s.LastAnsweredAt)
              .Select(s => s.QuestionId);

    public bool IsMastered(QuestionState s) => s.ConsecutiveCorrect >= CorrectStreakToMaster;

    internal static IReadOnlyDictionary<string, QuestionState> BuildStates(IEnumerable<AnswerRecord> answers)
    {
        var result = new Dictionary<string, QuestionState>();
        foreach (var group in answers.GroupBy(a => a.QuestionId))
        {
            var ordered = group.OrderBy(a => a.AnsweredAt).ToList();
            var streak = 0;
            foreach (var a in ordered) streak = a.IsCorrect ? streak + 1 : 0;
            var last = ordered[^1];
            result[group.Key] = new QuestionState(
                group.Key,
                last.CategoryId,
                ordered.Count,
                ordered.Count(a => a.IsCorrect),
                streak,
                last.IsCorrect,
                last.AnsweredAt);
        }
        return result;
    }
}
