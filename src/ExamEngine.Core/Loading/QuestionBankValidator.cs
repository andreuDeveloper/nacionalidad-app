using ExamEngine.Core.Models;

namespace ExamEngine.Core.Loading;

/// <summary>Comprobaciones de coherencia de un banco. Devuelve la lista de errores (vacía si todo está bien).</summary>
public static class QuestionBankValidator
{
    public static IReadOnlyList<string> Validate(QuestionBank bank)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(bank.Id)) errors.Add("El banco no tiene 'id'.");

        var categoryIds = new HashSet<string>();
        foreach (var c in bank.Categories)
        {
            if (string.IsNullOrWhiteSpace(c.Id)) errors.Add("Hay una categoría sin 'id'.");
            else if (!categoryIds.Add(c.Id)) errors.Add($"Categoría duplicada: {c.Id}.");
        }

        var questionIds = new HashSet<string>();
        foreach (var q in bank.Questions)
        {
            var label = string.IsNullOrWhiteSpace(q.Id) ? "(sin id)" : q.Id;
            if (string.IsNullOrWhiteSpace(q.Id)) errors.Add("Hay una pregunta sin 'id'.");
            else if (!questionIds.Add(q.Id)) errors.Add($"Pregunta duplicada: {q.Id}.");

            if (!categoryIds.Contains(q.CategoryId))
                errors.Add($"Pregunta {label}: categoría desconocida '{q.CategoryId}'.");
            if (q.Prompt.IsEmpty)
                errors.Add($"Pregunta {label}: enunciado vacío.");
            if (q.Options.Count < 2)
                errors.Add($"Pregunta {label}: necesita al menos 2 opciones.");

            var optionIds = new HashSet<string>();
            foreach (var o in q.Options)
            {
                if (string.IsNullOrWhiteSpace(o.Id) || !optionIds.Add(o.Id))
                    errors.Add($"Pregunta {label}: opción con id vacío o repetido.");
                if (o.Text.IsEmpty)
                    errors.Add($"Pregunta {label}: la opción '{o.Id}' no tiene texto.");
            }
            if (!optionIds.Contains(q.CorrectOptionId))
                errors.Add($"Pregunta {label}: 'correctOptionId' ({q.CorrectOptionId}) no es ninguna de sus opciones.");
        }

        var examIds = new HashSet<string>();
        foreach (var e in bank.Exams)
        {
            if (string.IsNullOrWhiteSpace(e.Id) || !examIds.Add(e.Id))
                errors.Add($"Examen con id vacío o repetido: '{e.Id}'.");
            if (e.Sections.Count == 0)
                errors.Add($"Examen {e.Id}: no tiene secciones.");
            if (e.PassMark < 0 || e.PassMark > e.TotalQuestions)
                errors.Add($"Examen {e.Id}: la nota de corte ({e.PassMark}) no cuadra con {e.TotalQuestions} preguntas.");
            foreach (var s in e.Sections)
            {
                if (!categoryIds.Contains(s.CategoryId))
                {
                    errors.Add($"Examen {e.Id}: categoría desconocida '{s.CategoryId}'.");
                    continue;
                }
                var available = bank.Questions.Count(q => q.CategoryId == s.CategoryId);
                if (s.QuestionCount <= 0 || s.QuestionCount > available)
                    errors.Add($"Examen {e.Id}: pide {s.QuestionCount} preguntas de '{s.CategoryId}' y hay {available}.");
            }
        }

        return errors;
    }
}
