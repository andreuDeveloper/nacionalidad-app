using ExamEngine.Core.Loading;
using ExamEngine.Core.Models;

namespace ExamEngine.Core.Tests;

public class LoaderTests
{
    [Fact]
    public void Loads_small_bank_and_normalizes_plain_strings_to_default_language()
    {
        var bank = TestData.SmallBank();

        Assert.Equal("demo", bank.Id);
        Assert.Equal(10, bank.Questions.Count);
        Assert.Equal("Pregunta 1", bank.Questions[0].Prompt.Get("es"));
        Assert.True(bank.Questions[0].Prompt.Has("es"));
        Assert.Equal("Categoría B", bank.FindCategory("B")!.Title.Get("fr"));
        Assert.Equal(5, bank.FindExam("mock")!.TotalQuestions);
        Assert.Equal(TimeSpan.FromMinutes(10), bank.FindExam("mock")!.TimeLimit);
    }

    [Fact]
    public void Rejects_newer_schema_versions()
    {
        var ex = Assert.Throws<QuestionBankFormatException>(() =>
            QuestionBankLoader.Load("""{ "schemaVersion": 99, "id": "x", "categories": [], "questions": [] }"""));
        Assert.Contains("99", ex.Message);
    }

    [Fact]
    public void Rejects_missing_schema_version()
    {
        Assert.Throws<QuestionBankFormatException>(() => QuestionBankLoader.Load("""{ "id": "x" }"""));
    }

    [Fact]
    public void Rejects_invalid_json()
    {
        Assert.Throws<QuestionBankFormatException>(() => QuestionBankLoader.Load("{ no es json"));
    }

    [Fact]
    public void Reports_inconsistencies()
    {
        var json = """
        {
          "schemaVersion": 1, "id": "bad",
          "categories": [ { "id": "A", "title": "A" } ],
          "exams": [ { "id": "e", "passMark": 9, "sections": [ { "categoryId": "A", "questionCount": 5 } ] } ],
          "questions": [
            { "id": "q1", "categoryId": "A", "prompt": "P", "options": [ { "id": "a", "text": "x" }, { "id": "b", "text": "y" } ], "correctOptionId": "z" },
            { "id": "q1", "categoryId": "NOPE", "prompt": "", "options": [ { "id": "a", "text": "x" } ], "correctOptionId": "a" }
          ]
        }
        """;
        var ex = Assert.Throws<QuestionBankFormatException>(() => QuestionBankLoader.Load(json));
        Assert.Contains("correctOptionId", ex.Message);
        Assert.Contains("duplicada", ex.Message);
        Assert.Contains("NOPE", ex.Message);
        Assert.Contains("al menos 2 opciones", ex.Message);
        Assert.Contains("pide 5", ex.Message);
    }

    [Fact]
    public async Task Roundtrip_serialization_keeps_content()
    {
        var bank = TestData.SmallBank();
        var json = QuestionBankLoader.Serialize(bank);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        var again = await QuestionBankLoader.LoadAsync(stream);

        Assert.Equal(bank.Questions.Count, again.Questions.Count);
        Assert.Equal("Because 3", again.FindQuestion("q3")!.Explanation!.Get("en"));
    }
}
