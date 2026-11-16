using ExamEngine.Core.Models;

namespace ExamEngine.Core.Tests;

public class LocalizedTextTests
{
    private static readonly LocalizedText Text = new(new Dictionary<string, string>
    {
        ["es"] = "Hola",
        ["en"] = "Hello",
        ["ar"] = "",
    });

    [Theory]
    [InlineData("es", "Hola")]
    [InlineData("EN", "Hello")]
    [InlineData("en-GB", "Hello")]
    [InlineData("fr", "Hola")]  // cae al idioma de reserva
    [InlineData("ar", "Hola")]  // vacío cuenta como ausente
    public void Get_falls_back_sensibly(string lang, string expected) =>
        Assert.Equal(expected, Text.Get(lang, "es"));

    [Fact]
    public void Get_without_fallback_returns_first_available()
    {
        var t = LocalizedText.Of("ur", "سلام");
        Assert.Equal("سلام", t.Get("fr"));
    }

    [Fact]
    public void Empty_text_returns_empty_string() => Assert.Equal(string.Empty, new LocalizedText().Get("es", "es"));
}
