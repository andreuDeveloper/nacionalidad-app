using ExamEngine.Core.Progress;

namespace ExamEngine.Core.Tests;

internal sealed class InMemoryProgressStore : IProgressStore
{
    public List<AnswerRecord> Answers { get; } = [];
    public List<SessionRecord> Sessions { get; } = [];

    public Task AddAnswersAsync(IEnumerable<AnswerRecord> answers, CancellationToken ct = default)
    {
        Answers.AddRange(answers);
        return Task.CompletedTask;
    }

    public Task AddSessionAsync(SessionRecord session, CancellationToken ct = default)
    {
        Sessions.Add(session);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AnswerRecord>> GetAnswersAsync(string bankId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AnswerRecord>>(Answers.Where(a => a.BankId == bankId).ToList());

    public Task<IReadOnlyList<SessionRecord>> GetSessionsAsync(string bankId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SessionRecord>>(Sessions.Where(s => s.BankId == bankId).ToList());

    public Task ResetAsync(string bankId, CancellationToken ct = default)
    {
        Answers.RemoveAll(a => a.BankId == bankId);
        Sessions.RemoveAll(s => s.BankId == bankId);
        return Task.CompletedTask;
    }
}
