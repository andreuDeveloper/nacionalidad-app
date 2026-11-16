using ExamEngine.Core.Models;
using Nacionalidad.App.Localization;

namespace Nacionalidad.App.Services;

/// <summary>Textos derivados del banco, localizados para la interfaz.</summary>
public static class BankText
{
    /// <summary>Título de categoría: primero el .resx (Cat_&lt;id&gt;), si no, el del banco.</summary>
    public static string CategoryTitle(QuestionBank bank, string categoryId, string language)
    {
        var key = $"Cat_{categoryId}";
        var fromResx = Translator.Instance[key];
        if (!fromResx.StartsWith('[')) return fromResx;
        return bank.FindCategory(categoryId)?.Title.Get(language, bank.DefaultLanguage) ?? categoryId;
    }

    /// <summary>Número de tarea a partir del orden de la categoría.</summary>
    public static int CategoryNumber(QuestionBank bank, string categoryId) =>
        bank.FindCategory(categoryId)?.Order ?? 0;

    public static string Percent(double ratio) =>
        string.Format(Translator.Instance.Culture, "{0:P0}", ratio);

    public static string Duration(TimeSpan t) =>
        t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"mm\:ss");
}
