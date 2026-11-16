using ExamEngine.Core.Progress;
using ExamEngine.Core.Sessions;
using ExamEngine.Core.Statistics;
using Microsoft.Extensions.Time.Testing;

namespace ExamEngine.Core.Tests;

public class ProgressTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero));
    private readonly InMemoryProgressStore _store = new();

    private void Answer(string qid, bool correct, string category = "A")
    {
        _time.Advance(TimeSpan.FromMinutes(1));
        _store.Answers.Add(new AnswerRecord("demo", qid, category, correct, SessionMode.Practice, _time.GetUtcNow()));
    }

    [Fact]
    public async Task Failed_question_stays_in_review_until_streak_reached()
    {
        var tracker = new ProgressTracker(_store, correctStreakToClear: 2);

        Answer("q1", false);
        Assert.Equal(["q1"], await tracker.GetPendingReviewAsync("demo"));

        Answer("q1", true);
        Assert.Equal(["q1"], await tracker.GetPendingReviewAsync("demo"));

        Answer("q1", true);
        Assert.Empty(await tracker.GetPendingReviewAsync("demo"));

        Answer("q1", false); // vuelve a fallar: vuelve al repaso
        Assert.Equal(["q1"], await tracker.GetPendingReviewAsync("demo"));
    }

    [Fact]
    public async Task Never_failed_questions_are_not_in_review()
    {
        var tracker = new ProgressTracker(_store);
        Answer("q1", true);
        Assert.Empty(await tracker.GetPendingReviewAsync("demo"));
    }

    [Fact]
    public async Task Review_is_sorted_by_most_failed_first()
    {
        var tracker = new ProgressTracker(_store);
        Answer("q1", false);
        Answer("q2", false);
        Answer("q2", false);
        Assert.Equal(["q2", "q1"], await tracker.GetPendingReviewAsync("demo"));
    }

    [Fact]
    public async Task Record_session_stores_answers_and_summary()
    {
        var tracker = new ProgressTracker(_store);
        var s = new SessionFactory(TestData.SmallBank(), new Random(1), _time).CreateMockExam("mock");
        s.Answer(0, "b");
        s.Answer(1, "a");
        await tracker.RecordAsync(s.Finish());

        Assert.Equal(2, _store.Answers.Count);       // las no contestadas no cuentan
        Assert.Single(_store.Sessions);
        Assert.Equal(SessionMode.MockExam, _store.Sessions[0].Mode);
        Assert.False(_store.Sessions[0].Passed);
    }

    [Fact]
    public async Task Record_session_without_answers_when_already_saved_one_by_one()
    {
        var tracker = new ProgressTracker(_store);
        var s = new SessionFactory(TestData.SmallBank(), new Random(1), _time).CreatePractice("A");
        s.Answer(0, "b");
        await tracker.RecordAnswerAsync("demo", s.Items[0], s.Mode);
        await tracker.RecordAsync(s.Finish(), includeAnswers: false);
        Assert.Single(_store.Answers);
        Assert.Single(_store.Sessions);
    }

    [Fact]
    public async Task Statistics_by_category()
    {
        var bank = TestData.SmallBank();
        var tracker = new ProgressTracker(_store, correctStreakToClear: 2, correctStreakToMaster: 2);
        Answer("q1", true); Answer("q1", true);        // dominada
        Answer("q2", false);                           // pendiente
        Answer("q7", true, "B");
        Answer("obsoleta", false);                     // ya no está en el banco: se ignora
        _store.Sessions.Add(new SessionRecord(Guid.NewGuid(), "demo", SessionMode.MockExam, "mock", null, 4, 5, true, _time.GetUtcNow(), _time.GetUtcNow()));

        var stats = StatisticsCalculator.Compute(bank, await _store.GetAnswersAsync("demo"), await _store.GetSessionsAsync("demo"), tracker,
            DateOnly.FromDateTime(_time.GetUtcNow().ToLocalTime().Date));

        Assert.Equal(10, stats.TotalQuestions);
        Assert.Equal(3, stats.Seen);
        Assert.Equal(1, stats.Mastered);
        Assert.Equal(1, stats.PendingReview);
        Assert.Equal(4, stats.Attempts);
        Assert.Equal(1, stats.MockExamsTaken);
        Assert.Equal(1, stats.MockExamsPassed);
        Assert.Equal(0.8, stats.LastMockExamRatio);
        Assert.Equal(1, stats.StudyStreakDays);

        var a = stats.Categories.Single(c => c.CategoryId == "A");
        Assert.Equal(6, a.TotalQuestions);
        Assert.Equal(2, a.Seen);
        Assert.Equal(2.0 / 3, a.Accuracy, 3);
        Assert.Equal(1, stats.Categories.Single(c => c.CategoryId == "B").Seen);
    }

    [Fact]
    public void Study_streak_counts_consecutive_days()
    {
        var today = new DateOnly(2026, 5, 10);
        DateTimeOffset At(int day) => new DateTimeOffset(2026, 5, day, 12, 0, 0, TimeSpan.Zero).ToLocalTime();

        Assert.Equal(3, StatisticsCalculator.StudyStreak([At(10), At(9), At(8), At(6)], today));
        Assert.Equal(2, StatisticsCalculator.StudyStreak([At(9), At(8)], today));  // ayer todavía cuenta
        Assert.Equal(0, StatisticsCalculator.StudyStreak([At(7)], today));
        Assert.Equal(0, StatisticsCalculator.StudyStreak([], today));
    }
}
