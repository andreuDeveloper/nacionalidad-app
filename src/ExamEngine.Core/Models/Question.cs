namespace ExamEngine.Core.Models;

/// <summary>Pregunta de respuesta única con opciones cerradas.</summary>
public sealed class Question
{
    /// <summary>Identificador estable. No reutilizar nunca un id para otra pregunta: el progreso se guarda por id.</summary>
    public string Id { get; set; } = string.Empty;

    public string CategoryId { get; set; } = string.Empty;

    public LocalizedText Prompt { get; set; } = new();

    public List<AnswerOption> Options { get; set; } = [];

    public string CorrectOptionId { get; set; } = string.Empty;

    /// <summary>Explicación de la respuesta correcta, en tantos idiomas como haya.</summary>
    public LocalizedText? Explanation { get; set; }

    /// <summary>Si es false, el orden de las opciones es significativo (p. ej. Verdadero/Falso) y no se baraja.</summary>
    public bool ShuffleOptions { get; set; } = true;

    public List<string> Tags { get; set; } = [];

    /// <summary>Referencia libre a la fuente (página del manual, etc.).</summary>
    public string? Reference { get; set; }

    public AnswerOption? FindOption(string optionId) => Options.FirstOrDefault(o => o.Id == optionId);

    public bool IsCorrect(string? optionId) => optionId is not null && optionId == CorrectOptionId;
}

public sealed class AnswerOption
{
    public string Id { get; set; } = string.Empty;
    public LocalizedText Text { get; set; } = new();
}
