using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ExamEngine.Core.Models;

namespace ExamEngine.Core.Loading;

/// <summary>
/// Carga y valida bancos de preguntas en JSON.
/// </summary>
public static class QuestionBankLoader
{
    /// <summary>Versión de formato que entiende esta versión del motor.</summary>
    public const int SupportedSchemaVersion = 1;

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = BankJsonContext.Default,
    };

    private static JsonTypeInfo<QuestionBank> BankTypeInfo =>
        (JsonTypeInfo<QuestionBank>)JsonOptions.GetTypeInfo(typeof(QuestionBank));

    public static async Task<QuestionBank> LoadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        QuestionBank? bank;
        try
        {
            bank = await JsonSerializer.DeserializeAsync(stream, BankTypeInfo, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            throw new QuestionBankFormatException($"JSON no válido: {ex.Message}", ex);
        }
        return Validate(bank);
    }

    public static QuestionBank Load(string json)
    {
        QuestionBank? bank;
        try
        {
            bank = JsonSerializer.Deserialize(json, BankTypeInfo);
        }
        catch (JsonException ex)
        {
            throw new QuestionBankFormatException($"JSON no válido: {ex.Message}", ex);
        }
        return Validate(bank);
    }

    public static string Serialize(QuestionBank bank) => JsonSerializer.Serialize(bank, BankTypeInfo);

    private static QuestionBank Validate(QuestionBank? bank)
    {
        if (bank is null) throw new QuestionBankFormatException("El fichero está vacío.");
        if (bank.SchemaVersion <= 0)
            throw new QuestionBankFormatException("Falta 'schemaVersion'.");
        if (bank.SchemaVersion > SupportedSchemaVersion)
            throw new QuestionBankFormatException(
                $"El banco usa schemaVersion {bank.SchemaVersion} y este motor solo entiende hasta la {SupportedSchemaVersion}. Actualiza la app.");

        var errors = QuestionBankValidator.Validate(bank);
        if (errors.Count > 0)
            throw new QuestionBankFormatException("Banco de preguntas inconsistente:\n- " + string.Join("\n- ", errors));

        NormalizeInvariantTexts(bank);
        return bank;
    }

    /// <summary>Las cadenas simples del JSON se asignan al idioma por defecto del banco.</summary>
    private static void NormalizeInvariantTexts(QuestionBank bank)
    {
        LocalizedText Fix(LocalizedText t) =>
            t.Values.ContainsKey(LocalizedText.InvariantKey) && !t.Has(bank.DefaultLanguage)
                ? new LocalizedText(t.Values
                    .Where(kv => kv.Key != LocalizedText.InvariantKey)
                    .Append(new KeyValuePair<string, string>(bank.DefaultLanguage, t.Values[LocalizedText.InvariantKey]))
                    .ToDictionary(kv => kv.Key, kv => kv.Value))
                : t;

        bank.Title = Fix(bank.Title);
        foreach (var c in bank.Categories)
        {
            c.Title = Fix(c.Title);
            if (c.Description is not null) c.Description = Fix(c.Description);
        }
        foreach (var e in bank.Exams) e.Title = Fix(e.Title);
        foreach (var q in bank.Questions)
        {
            q.Prompt = Fix(q.Prompt);
            if (q.Explanation is not null) q.Explanation = Fix(q.Explanation);
            foreach (var o in q.Options) o.Text = Fix(o.Text);
        }
    }
}

public sealed class QuestionBankFormatException : Exception
{
    public QuestionBankFormatException(string message, Exception? inner = null) : base(message, inner) { }
}
