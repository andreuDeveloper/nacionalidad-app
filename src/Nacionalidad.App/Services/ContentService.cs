using ExamEngine.Core.Loading;
using ExamEngine.Core.Models;

namespace Nacionalidad.App.Services;

/// <summary>Carga (una sola vez) los bancos de preguntas empaquetados en Content/.</summary>
public sealed class ContentService
{
    public const string CcseBankFile = "Content/ccse.json";
    public const string CcseExamId = "ccse-oficial";

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, QuestionBank> _cache = [];

    public Task<QuestionBank> GetCcseAsync() => GetAsync(CcseBankFile);

    public async Task<QuestionBank> GetAsync(string file)
    {
        await _lock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(file, out var cached)) return cached;
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(file);
            var bank = await QuestionBankLoader.LoadAsync(stream);
            _cache[file] = bank;
            return bank;
        }
        finally
        {
            _lock.Release();
        }
    }
}
