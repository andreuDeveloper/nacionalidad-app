namespace ExamEngine.Core.Sessions;

public sealed record QuestionOutcome(string QuestionId, string CategoryId, string? SelectedOptionId, string CorrectOptionId, bool IsCorrect)
{
    public bool IsAnswered => SelectedOptionId is not null;
}

public sealed record CategoryScore(string CategoryId, int Correct, int Total)
{
    public double Ratio => Total == 0 ? 0 : (double)Correct / Total;
}

/// <summary>Resultado inmutable de una sesión terminada.</summary>
public sealed record ExamResult
{
    public required Guid SessionId { get; init; }
    public required string BankId { get; init; }
    public required SessionMode Mode { get; init; }
    public string? BlueprintId { get; init; }
    public string? CategoryId { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset FinishedAt { get; init; }
    public required IReadOnlyList<QuestionOutcome> Outcomes { get; init; }
    public int? PassMark { get; init; }

    public int Total => Outcomes.Count;
    public int Correct => Outcomes.Count(o => o.IsCorrect);
    public int Wrong => Outcomes.Count(o => o.IsAnswered && !o.IsCorrect);
    public int Unanswered => Outcomes.Count(o => !o.IsAnswered);
    public double Ratio => Total == 0 ? 0 : (double)Correct / Total;
    public TimeSpan Duration => FinishedAt - StartedAt;

    /// <summary>null cuando la sesión no tiene nota de corte (práctica).</summary>
    public bool? Passed => PassMark is { } p ? Correct >= p : null;

    public IReadOnlyList<CategoryScore> ByCategory =>
        Outcomes.GroupBy(o => o.CategoryId)
            .Select(g => new CategoryScore(g.Key, g.Count(o => o.IsCorrect), g.Count()))
            .ToList();

    internal static ExamResult From(ExamSession s) => new()
    {
        SessionId = s.Id,
        BankId = s.BankId,
        Mode = s.Mode,
        BlueprintId = s.BlueprintId,
        CategoryId = s.CategoryId,
        StartedAt = s.StartedAt,
        FinishedAt = s.FinishedAt ?? s.StartedAt,
        PassMark = s.PassMark,
        Outcomes = s.Items.Select(i => new QuestionOutcome(
            i.Question.Id, i.Question.CategoryId, i.SelectedOptionId, i.Question.CorrectOptionId, i.IsCorrect)).ToList(),
    };
}
