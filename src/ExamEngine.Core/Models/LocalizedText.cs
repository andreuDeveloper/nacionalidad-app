using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExamEngine.Core.Models;

/// <summary>
/// Texto con traducciones indexadas por código de idioma ISO 639-1 ("es", "en", "ar"...).
/// En JSON puede escribirse como cadena simple (se asume el idioma por defecto del banco)
/// o como objeto <c>{ "es": "...", "en": "..." }</c>.
/// </summary>
[JsonConverter(typeof(LocalizedTextJsonConverter))]
public sealed class LocalizedText
{
    /// <summary>Clave usada cuando el JSON trae una cadena simple sin idioma.</summary>
    public const string InvariantKey = "";

    private readonly Dictionary<string, string> _values;

    public LocalizedText() : this(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)) { }

    public LocalizedText(IDictionary<string, string> values)
    {
        _values = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
    }

    public static LocalizedText Of(string language, string text) =>
        new(new Dictionary<string, string> { [language] = text });

    public IReadOnlyDictionary<string, string> Values => _values;

    public bool IsEmpty => _values.Count == 0 || _values.Values.All(string.IsNullOrWhiteSpace);

    public bool Has(string language) =>
        _values.TryGetValue(language, out var v) && !string.IsNullOrWhiteSpace(v);

    public LocalizedText With(string language, string text)
    {
        var copy = new Dictionary<string, string>(_values, StringComparer.OrdinalIgnoreCase) { [language] = text };
        return new LocalizedText(copy);
    }

    /// <summary>
    /// Devuelve el texto en <paramref name="language"/>; si no existe, en <paramref name="fallbackLanguage"/>;
    /// si tampoco, el primero disponible. Nunca devuelve null.
    /// </summary>
    public string Get(string language, string? fallbackLanguage = null)
    {
        if (TryGetNonEmpty(language, out var text)) return text;

        // "en-GB" -> "en"
        var dash = language.IndexOf('-');
        if (dash > 0 && TryGetNonEmpty(language[..dash], out text)) return text;

        if (fallbackLanguage is not null && TryGetNonEmpty(fallbackLanguage, out text)) return text;
        if (TryGetNonEmpty(InvariantKey, out text)) return text;

        return _values.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
    }

    private bool TryGetNonEmpty(string key, out string text)
    {
        if (_values.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
        {
            text = v;
            return true;
        }
        text = string.Empty;
        return false;
    }

    public override string ToString() => Get("es");
}

internal sealed class LocalizedTextJsonConverter : JsonConverter<LocalizedText>
{
    public override LocalizedText Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return LocalizedText.Of(LocalizedText.InvariantKey, reader.GetString() ?? string.Empty);
            case JsonTokenType.StartObject:
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject) return new LocalizedText(dict);
                    if (reader.TokenType != JsonTokenType.PropertyName)
                        throw new JsonException("Se esperaba un código de idioma.");
                    var key = reader.GetString()!;
                    reader.Read();
                    dict[key] = reader.TokenType == JsonTokenType.Null ? string.Empty : reader.GetString() ?? string.Empty;
                }
                throw new JsonException("Objeto de texto localizado sin cerrar.");
            case JsonTokenType.Null:
                return new LocalizedText();
            default:
                throw new JsonException($"Token inesperado para texto localizado: {reader.TokenType}.");
        }
    }

    public override void Write(Utf8JsonWriter writer, LocalizedText value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (k, v) in value.Values) writer.WriteString(k, v);
        writer.WriteEndObject();
    }
}
