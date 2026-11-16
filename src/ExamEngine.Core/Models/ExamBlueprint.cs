namespace ExamEngine.Core.Models;

/// <summary>
/// Formato de un simulacro: cuántas preguntas de cada categoría, tiempo y nota de corte.
/// </summary>
public sealed class ExamBlueprint
{
    public string Id { get; set; } = string.Empty;

    public LocalizedText Title { get; set; } = new();

    /// <summary>Tiempo máximo en minutos. 0 o negativo = sin límite.</summary>
    public int TimeLimitMinutes { get; set; }

    /// <summary>Número mínimo de aciertos para aprobar.</summary>
    public int PassMark { get; set; }

    public List<ExamSection> Sections { get; set; } = [];

    public int TotalQuestions => Sections.Sum(s => s.QuestionCount);

    public TimeSpan? TimeLimit => TimeLimitMinutes > 0 ? TimeSpan.FromMinutes(TimeLimitMinutes) : null;
}

public sealed class ExamSection
{
    public string CategoryId { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
}
