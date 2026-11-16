using ExamEngine.Core.Progress;
using ExamEngine.Core.Sessions;

namespace Nacionalidad.App.Data;

/// <summary>Implementación de <see cref="IProgressStore"/> del motor sobre SQLite.</summary>
public sealed class SqliteProgressStore(AppDatabase database) : IProgressStore
{
    public async Task AddAnswersAsync(IEnumerable<AnswerRecord> answers, CancellationToken ct = default)
    {
        var db = await database.GetAsync();
        var rows = answers.Select(a => new AnswerRow
        {
            BankId = a.BankId,
            QuestionId = a.QuestionId,
            CategoryId = a.CategoryId,
            IsCorrect = a.IsCorrect,
            Mode = (int)a.Mode,
            AnsweredAtUtcTicks = a.AnsweredAt.UtcTicks,
        }).ToList();
        if (rows.Count > 0) await db.InsertAllAsync(rows);
    }

    public async Task AddSessionAsync(SessionRecord s, CancellationToken ct = default)
    {
        var db = await database.GetAsync();
        await db.InsertOrReplaceAsync(new SessionRow
        {
            SessionId = s.SessionId.ToString(),
            BankId = s.BankId,
            Mode = (int)s.Mode,
            BlueprintId = s.BlueprintId,
            CategoryId = s.CategoryId,
            Correct = s.Correct,
            Total = s.Total,
            Passed = s.Passed is null ? null : s.Passed.Value ? 1 : 0,
            StartedAtUtcTicks = s.StartedAt.UtcTicks,
            FinishedAtUtcTicks = s.FinishedAt.UtcTicks,
        });
    }

    public async Task<IReadOnlyList<AnswerRecord>> GetAnswersAsync(string bankId, CancellationToken ct = default)
    {
        var db = await database.GetAsync();
        var rows = await db.Table<AnswerRow>().Where(r => r.BankId == bankId).ToListAsync();
        return rows.Select(r => new AnswerRecord(
            r.BankId, r.QuestionId, r.CategoryId, r.IsCorrect, (SessionMode)r.Mode, FromTicks(r.AnsweredAtUtcTicks))).ToList();
    }

    public async Task<IReadOnlyList<SessionRecord>> GetSessionsAsync(string bankId, CancellationToken ct = default)
    {
        var db = await database.GetAsync();
        var rows = await db.Table<SessionRow>().Where(r => r.BankId == bankId).ToListAsync();
        return rows.Select(r => new SessionRecord(
            Guid.Parse(r.SessionId), r.BankId, (SessionMode)r.Mode, r.BlueprintId, r.CategoryId, r.Correct, r.Total,
            r.Passed is null ? null : r.Passed == 1, FromTicks(r.StartedAtUtcTicks), FromTicks(r.FinishedAtUtcTicks))).ToList();
    }

    public async Task ResetAsync(string bankId, CancellationToken ct = default)
    {
        var db = await database.GetAsync();
        await db.Table<AnswerRow>().DeleteAsync(r => r.BankId == bankId);
        await db.Table<SessionRow>().DeleteAsync(r => r.BankId == bankId);
    }

    private static DateTimeOffset FromTicks(long utcTicks) => new(utcTicks, TimeSpan.Zero);
}
