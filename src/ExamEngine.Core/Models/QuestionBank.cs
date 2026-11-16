namespace ExamEngine.Core.Models;

/// <summary>
/// Banco de preguntas tal y como se guarda en JSON. Genérico: no sabe nada de un examen concreto.
/// </summary>
public sealed class QuestionBank
{
    /// <summary>Versión del formato del fichero (no del contenido). Ver <see cref="Loading.QuestionBankLoader.SupportedSchemaVersion"/>.</summary>
    public int SchemaVersion { get; set; }

    /// <summary>Identificador estable del banco, p. ej. "ccse".</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Versión del contenido (p. ej. "2026.1"). Cambia cada vez que se actualizan preguntas.</summary>
    public string ContentVersion { get; set; } = string.Empty;

    /// <summary>Idioma en el que están escritos los enunciados y que se usa como último recurso.</summary>
    public string DefaultLanguage { get; set; } = "es";

    public LocalizedText Title { get; set; } = new();

    /// <summary>Fuente del contenido (manual oficial, autoría propia...).</summary>
    public string? Source { get; set; }

    public List<Category> Categories { get; set; } = [];

    public List<Question> Questions { get; set; } = [];

    /// <summary>Formatos de examen (simulacros) definidos para este banco.</summary>
    public List<ExamBlueprint> Exams { get; set; } = [];

    public Category? FindCategory(string id) => Categories.FirstOrDefault(c => c.Id == id);

    public Question? FindQuestion(string id) => Questions.FirstOrDefault(q => q.Id == id);

    public ExamBlueprint? FindExam(string id) => Exams.FirstOrDefault(e => e.Id == id);

    public IEnumerable<Question> QuestionsIn(string categoryId) => Questions.Where(q => q.CategoryId == categoryId);
}

public sealed class Category
{
    public string Id { get; set; } = string.Empty;
    public LocalizedText Title { get; set; } = new();
    public LocalizedText? Description { get; set; }
    public int Order { get; set; }
}
