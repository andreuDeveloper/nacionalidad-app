using ExamEngine.Core.Models;

namespace ExamEngine.Core.Sessions;

/// <summary>
/// Crea sesiones (práctica, simulacro, repaso) a partir de un banco.
/// El orden aleatorio se controla con <see cref="Random"/> para poder testearlo con semilla.
/// </summary>
public sealed class SessionFactory
{
    private readonly QuestionBank _bank;
    private readonly Random _random;
    private readonly TimeProvider _time;

    public SessionFactory(QuestionBank bank, Random? random = null, TimeProvider? time = null)
    {
        _bank = bank;
        _random = random ?? Random.Shared;
        _time = time ?? TimeProvider.System;
    }

    public QuestionBank Bank => _bank;

    /// <summary>
    /// Práctica de una categoría (o de todo el banco si <paramref name="categoryId"/> es null).
    /// </summary>
    /// <param name="maxQuestions">Límite de preguntas; null = todas.</param>
    /// <param name="prioritize">Ids que deben ir primero (p. ej. preguntas nunca vistas).</param>
    public ExamSession CreatePractice(
        string? categoryId,
        int? maxQuestions = null,
        bool shuffle = true,
        IReadOnlySet<string>? prioritize = null)
    {
        var pool = (categoryId is null ? _bank.Questions : _bank.QuestionsIn(categoryId)).ToList();
        if (pool.Count == 0) throw new InvalidOperationException($"No hay preguntas para la categoría '{categoryId}'.");

        IEnumerable<Question> ordered = shuffle ? Shuffled(pool) : pool;
        if (prioritize is { Count: > 0 })
            ordered = ordered.OrderBy(q => prioritize.Contains(q.Id) ? 0 : 1);
        if (maxQuestions is > 0) ordered = ordered.Take(maxQuestions.Value);

        return Build(SessionMode.Practice, ordered, timeLimit: null, passMark: null, blueprintId: null, categoryId);
    }

    /// <summary>Simulacro con el formato de <paramref name="blueprintId"/>.</summary>
    public ExamSession CreateMockExam(string blueprintId)
    {
        var bp = _bank.FindExam(blueprintId) ?? throw new ArgumentException($"No existe el examen '{blueprintId}'.", nameof(blueprintId));
        var selected = new List<Question>(bp.TotalQuestions);
        foreach (var section in bp.Sections)
        {
            var pool = _bank.QuestionsIn(section.CategoryId).ToList();
            selected.AddRange(Shuffled(pool).Take(section.QuestionCount));
        }
        // Se respeta el orden por secciones, como en el examen real.
        return Build(SessionMode.MockExam, selected, bp.TimeLimit, bp.PassMark, bp.Id, categoryId: null);
    }

    /// <summary>Repaso de una lista de preguntas (normalmente, las falladas).</summary>
    public ExamSession CreateReview(IEnumerable<string> questionIds, int? maxQuestions = null)
    {
        var ids = questionIds.Distinct().ToList();
        var pool = ids.Select(_bank.FindQuestion).OfType<Question>().ToList();
        if (pool.Count == 0) throw new InvalidOperationException("No hay preguntas que repasar.");
        IEnumerable<Question> ordered = Shuffled(pool);
        if (maxQuestions is > 0) ordered = ordered.Take(maxQuestions.Value);
        return Build(SessionMode.Review, ordered, timeLimit: null, passMark: null, blueprintId: null, categoryId: null);
    }

    private ExamSession Build(SessionMode mode, IEnumerable<Question> questions, TimeSpan? timeLimit, int? passMark, string? blueprintId, string? categoryId)
    {
        var items = questions.Select(q => (q, (IReadOnlyList<AnswerOption>)(q.ShuffleOptions ? Shuffled(q.Options) : q.Options.ToList())));
        return new ExamSession(_bank.Id, mode, items.ToList(), timeLimit, passMark, blueprintId, categoryId, _time);
    }

    private List<T> Shuffled<T>(IEnumerable<T> source)
    {
        var list = source.ToList();
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }
}
