using ExamEngine.Core.Loading;
using ExamEngine.Core.Sessions;

namespace ExamEngine.Core.Tests;

/// <summary>
/// Comprueba que los bancos JSON que lleva la app (src/Nacionalidad.App/Content) cargan sin errores.
/// Son genéricos: no saben qué examen es, solo que el fichero tiene que ser válido.
/// </summary>
public class AppBankTests
{
    public static TheoryData<string> Banks()
    {
        var data = new TheoryData<string>();
        var dir = Path.Combine(AppContext.BaseDirectory, "Banks");
        if (Directory.Exists(dir))
            foreach (var f in Directory.GetFiles(dir, "*.json")) data.Add(Path.GetFileName(f));
        return data;
    }

    [Theory]
    [MemberData(nameof(Banks))]
    public async Task Bank_loads_and_every_exam_can_be_generated(string file)
    {
        await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Banks", file));
        var bank = await QuestionBankLoader.LoadAsync(stream);

        Assert.NotEmpty(bank.Questions);
        Assert.All(bank.Questions, q => Assert.False(q.Prompt.IsEmpty));
        Assert.All(bank.Questions, q => Assert.True(q.Explanation is { IsEmpty: false }, $"{q.Id} sin explicación"));

        var factory = new SessionFactory(bank, new Random(1));
        foreach (var exam in bank.Exams)
        {
            var s = factory.CreateMockExam(exam.Id);
            Assert.Equal(exam.TotalQuestions, s.Count);
        }
        foreach (var c in bank.Categories)
            Assert.NotEmpty(factory.CreatePractice(c.Id).Items);
    }
}
