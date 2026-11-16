using ExamEngine.Core.Models;

namespace ExamEngine.Core.Sessions;

public enum SessionMode
{
    /// <summary>Práctica libre: se ve la corrección tras cada pregunta.</summary>
    Practice,
    /// <summary>Simulacro: con tiempo, corrección al final.</summary>
    MockExam,
    /// <summary>Repaso de preguntas falladas.</summary>
    Review,
}

/// <summary>Una pregunta dentro de una sesión, con el orden de opciones ya decidido.</summary>
public sealed class SessionItem
{
    internal SessionItem(int index, Question question, IReadOnlyList<AnswerOption> options)
    {
        Index = index;
        Question = question;
        Options = options;
    }

    public int Index { get; }
    public Question Question { get; }

    /// <summary>Opciones en el orden en el que se muestran (posiblemente barajadas).</summary>
    public IReadOnlyList<AnswerOption> Options { get; }

    public string? SelectedOptionId { get; internal set; }
    public DateTimeOffset? AnsweredAt { get; internal set; }

    public bool IsAnswered => SelectedOptionId is not null;
    public bool IsCorrect => Question.IsCorrect(SelectedOptionId);
}

/// <summary>
/// Sesión de preguntas en curso: práctica, simulacro o repaso.
/// No es thread-safe; pensada para usarse desde el hilo de UI.
/// </summary>
public sealed class ExamSession
{
    private readonly List<SessionItem> _items;
    private readonly TimeProvider _time;

    internal ExamSession(
        string bankId,
        SessionMode mode,
        IEnumerable<(Question Question, IReadOnlyList<AnswerOption> Options)> items,
        TimeSpan? timeLimit,
        int? passMark,
        string? blueprintId,
        string? categoryId,
        TimeProvider time)
    {
        Id = Guid.NewGuid();
        BankId = bankId;
        Mode = mode;
        TimeLimit = timeLimit;
        PassMark = passMark;
        BlueprintId = blueprintId;
        CategoryId = categoryId;
        _time = time;
        _items = items.Select((x, i) => new SessionItem(i, x.Question, x.Options)).ToList();
        StartedAt = time.GetUtcNow();
    }

    public Guid Id { get; }
    public string BankId { get; }
    public SessionMode Mode { get; }
    public string? BlueprintId { get; }
    public string? CategoryId { get; }
    public TimeSpan? TimeLimit { get; }
    public int? PassMark { get; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset? FinishedAt { get; private set; }

    public IReadOnlyList<SessionItem> Items => _items;
    public int Count => _items.Count;
    public int AnsweredCount => _items.Count(i => i.IsAnswered);
    public int CorrectCount => _items.Count(i => i.IsCorrect);

    public bool IsFinished => FinishedAt is not null;

    public TimeSpan Elapsed => (FinishedAt ?? _time.GetUtcNow()) - StartedAt;

    /// <summary>Tiempo restante; null si la sesión no tiene límite.</summary>
    public TimeSpan? Remaining
    {
        get
        {
            if (TimeLimit is null) return null;
            var r = TimeLimit.Value - Elapsed;
            return r < TimeSpan.Zero ? TimeSpan.Zero : r;
        }
    }

    public bool IsTimeUp => Remaining is { } r && r <= TimeSpan.Zero;

    /// <summary>
    /// Registra (o cambia) la respuesta a una pregunta. En práctica y repaso la primera respuesta es definitiva;
    /// en simulacro se puede cambiar hasta entregar.
    /// </summary>
    /// <returns>true si la respuesta queda registrada.</returns>
    public bool Answer(int index, string optionId)
    {
        if (IsFinished || IsTimeUp) return false;
        if ((uint)index >= (uint)_items.Count) throw new ArgumentOutOfRangeException(nameof(index));

        var item = _items[index];
        if (item.Question.FindOption(optionId) is null)
            throw new ArgumentException($"La opción '{optionId}' no pertenece a la pregunta {item.Question.Id}.", nameof(optionId));
        if (item.IsAnswered && Mode != SessionMode.MockExam) return false;

        item.SelectedOptionId = optionId;
        item.AnsweredAt = _time.GetUtcNow();
        return true;
    }

    /// <summary>Cierra la sesión y calcula el resultado. Es idempotente.</summary>
    public ExamResult Finish()
    {
        if (FinishedAt is null)
        {
            var now = _time.GetUtcNow();
            // Si se pasó el tiempo, la hora de fin es la del límite.
            if (TimeLimit is { } limit && now - StartedAt > limit) now = StartedAt + limit;
            FinishedAt = now;
        }
        return ExamResult.From(this);
    }
}
