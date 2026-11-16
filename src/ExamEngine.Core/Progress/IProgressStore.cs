using ExamEngine.Core.Sessions;

namespace ExamEngine.Core.Progress;

/// <summary>Una respuesta del usuario a una pregunta, guardada para estadísticas y repaso.</summary>
public sealed record AnswerRecord(
    string BankId,
    string QuestionId,
    string CategoryId,
    bool IsCorrect,
    SessionMode Mode,
    DateTimeOffset AnsweredAt);

/// <summary>Resumen guardado de una sesión terminada (sin el detalle por pregunta).</summary>
public sealed record SessionRecord(
    Guid SessionId,
    string BankId,
    SessionMode Mode,
    string? BlueprintId,
    string? CategoryId,
    int Correct,
    int Total,
    bool? Passed,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt)
{
    public static SessionRecord From(ExamResult r) =>
        new(r.SessionId, r.BankId, r.Mode, r.BlueprintId, r.CategoryId, r.Correct, r.Total, r.Passed, r.StartedAt, r.FinishedAt);
}

/// <summary>
/// Persistencia del progreso. El motor solo define el contrato; cada app lo implementa (SQLite, ficheros...).
/// </summary>
public interface IProgressStore
{
    Task AddAnswersAsync(IEnumerable<AnswerRecord> answers, CancellationToken ct = default);

    Task AddSessionAsync(SessionRecord session, CancellationToken ct = default);

    Task<IReadOnlyList<AnswerRecord>> GetAnswersAsync(string bankId, CancellationToken ct = default);

    Task<IReadOnlyList<SessionRecord>> GetSessionsAsync(string bankId, CancellationToken ct = default);

    /// <summary>Borra todo el progreso de un banco.</summary>
    Task ResetAsync(string bankId, CancellationToken ct = default);
}
