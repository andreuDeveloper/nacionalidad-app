using System.Text.Json.Serialization;
using ExamEngine.Core.Models;

namespace ExamEngine.Core.Loading;

/// <summary>
/// Serialización generada en compilación: las apps Android en Release se recortan (trimming)
/// y ahí System.Text.Json por reflexión está desactivado.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(QuestionBank))]
internal sealed partial class BankJsonContext : JsonSerializerContext;
