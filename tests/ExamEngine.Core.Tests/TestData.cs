using ExamEngine.Core.Loading;
using ExamEngine.Core.Models;

namespace ExamEngine.Core.Tests;

internal static class TestData
{
    /// <summary>Banco pequeño: 2 categorías (A con 6 preguntas, B con 4) y un examen de 3+2 con aprobado en 3.</summary>
    public static QuestionBank SmallBank()
    {
        var json = """
        {
          "schemaVersion": 1,
          "id": "demo",
          "contentVersion": "1.0",
          "defaultLanguage": "es",
          "title": "Demo",
          "categories": [
            { "id": "A", "title": { "es": "Categoría A", "en": "Category A" }, "order": 1 },
            { "id": "B", "title": "Categoría B", "order": 2 }
          ],
          "exams": [
            { "id": "mock", "title": "Simulacro", "timeLimitMinutes": 10, "passMark": 3,
              "sections": [ { "categoryId": "A", "questionCount": 3 }, { "categoryId": "B", "questionCount": 2 } ] }
          ],
          "questions": [
        """ + string.Join(",\n", Enumerable.Range(1, 10).Select(i => Question(i, i <= 6 ? "A" : "B"))) + """
          ]
        }
        """;
        return QuestionBankLoader.Load(json);
    }

    private static string Question(int i, string category) => $$"""
        {
          "id": "q{{i}}",
          "categoryId": "{{category}}",
          "prompt": "Pregunta {{i}}",
          "options": [ { "id": "a", "text": "A{{i}}" }, { "id": "b", "text": "B{{i}}" }, { "id": "c", "text": "C{{i}}" } ],
          "correctOptionId": "b",
          "explanation": { "es": "Porque sí {{i}}", "en": "Because {{i}}" }
        }
        """;
}
