using ExamEngine.Core.Sessions;
using Microsoft.Extensions.Time.Testing;

namespace ExamEngine.Core.Tests;

public class SessionTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero));

    private SessionFactory Factory(int seed = 1) => new(TestData.SmallBank(), new Random(seed), _time);

    [Fact]
    public void Practice_by_category_contains_only_that_category()
    {
        var s = Factory().CreatePractice("A");
        Assert.Equal(6, s.Count);
        Assert.All(s.Items, i => Assert.Equal("A", i.Question.CategoryId));
        Assert.Null(s.TimeLimit);
        Assert.Equal(SessionMode.Practice, s.Mode);
    }

    [Fact]
    public void Practice_respects_max_and_priority()
    {
        var prioritized = new HashSet<string> { "q5", "q6" };
        var s = Factory().CreatePractice("A", maxQuestions: 2, prioritize: prioritized);
        Assert.Equal(2, s.Count);
        Assert.All(s.Items, i => Assert.Contains(i.Question.Id, prioritized));
    }

    [Fact]
    public void Practice_without_category_uses_whole_bank()
    {
        Assert.Equal(10, Factory().CreatePractice(null).Count);
    }

    [Fact]
    public void Mock_exam_follows_blueprint_sections_in_order()
    {
        var s = Factory().CreateMockExam("mock");
        Assert.Equal(5, s.Count);
        Assert.Equal(["A", "A", "A", "B", "B"], s.Items.Select(i => i.Question.CategoryId));
        Assert.Equal(5, s.Items.Select(i => i.Question.Id).Distinct().Count());
        Assert.Equal(TimeSpan.FromMinutes(10), s.TimeLimit);
        Assert.Equal(3, s.PassMark);
    }

    [Fact]
    public void Options_are_shuffled_but_complete()
    {
        var s = Factory(seed: 42).CreatePractice(null);
        Assert.All(s.Items, i => Assert.Equal(["a", "b", "c"], i.Options.Select(o => o.Id).Order()));
        Assert.Contains(s.Items, i => !i.Options.Select(o => o.Id).SequenceEqual(["a", "b", "c"]));
    }

    [Fact]
    public void Options_are_not_shuffled_when_disabled()
    {
        var bank = TestData.SmallBank();
        foreach (var q in bank.Questions) q.ShuffleOptions = false;
        var s = new SessionFactory(bank, new Random(3), _time).CreatePractice(null);
        Assert.All(s.Items, i => Assert.Equal(["a", "b", "c"], i.Options.Select(o => o.Id)));
    }

    [Fact]
    public void Practice_answer_is_final_and_scored()
    {
        var s = Factory().CreatePractice("A");
        Assert.True(s.Answer(0, "b"));
        Assert.False(s.Answer(0, "a"));
        Assert.True(s.Items[0].IsCorrect);
        Assert.True(s.Answer(1, "c"));
        Assert.False(s.Items[1].IsCorrect);
        Assert.Equal(2, s.AnsweredCount);
        Assert.Equal(1, s.CorrectCount);
    }

    [Fact]
    public void Mock_exam_answers_can_change_until_finished()
    {
        var s = Factory().CreateMockExam("mock");
        Assert.True(s.Answer(0, "a"));
        Assert.True(s.Answer(0, "b"));
        Assert.True(s.Items[0].IsCorrect);
        s.Finish();
        Assert.False(s.Answer(1, "b"));
    }

    [Fact]
    public void Invalid_option_throws()
    {
        var s = Factory().CreatePractice("A");
        Assert.Throws<ArgumentException>(() => s.Answer(0, "zzz"));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.Answer(99, "a"));
    }

    [Fact]
    public void Mock_exam_pass_and_fail()
    {
        var pass = Factory().CreateMockExam("mock");
        for (var i = 0; i < 3; i++) pass.Answer(i, "b");
        pass.Answer(3, "a");
        var r = pass.Finish();
        Assert.Equal(3, r.Correct);
        Assert.Equal(1, r.Wrong);
        Assert.Equal(1, r.Unanswered);
        Assert.True(r.Passed);
        Assert.Equal([new CategoryScore("A", 3, 3), new CategoryScore("B", 0, 2)], r.ByCategory);

        var fail = Factory().CreateMockExam("mock");
        fail.Answer(0, "b");
        Assert.False(fail.Finish().Passed);
    }

    [Fact]
    public void Practice_result_has_no_pass_mark()
    {
        var s = Factory().CreatePractice("B");
        Assert.Null(s.Finish().Passed);
    }

    [Fact]
    public void Timer_counts_down_and_blocks_answers_when_time_is_up()
    {
        var s = Factory().CreateMockExam("mock");
        _time.Advance(TimeSpan.FromMinutes(4));
        Assert.Equal(TimeSpan.FromMinutes(6), s.Remaining);
        Assert.True(s.Answer(0, "b"));

        _time.Advance(TimeSpan.FromMinutes(7));
        Assert.True(s.IsTimeUp);
        Assert.Equal(TimeSpan.Zero, s.Remaining);
        Assert.False(s.Answer(1, "b"));

        var r = s.Finish();
        Assert.Equal(TimeSpan.FromMinutes(10), r.Duration); // se corta en el límite
    }

    [Fact]
    public void Finish_is_idempotent()
    {
        var s = Factory().CreatePractice("A");
        var r1 = s.Finish();
        _time.Advance(TimeSpan.FromMinutes(5));
        var r2 = s.Finish();
        Assert.Equal(r1.FinishedAt, r2.FinishedAt);
    }

    [Fact]
    public void Review_uses_only_known_ids()
    {
        var s = Factory().CreateReview(["q1", "q9", "no-existe", "q1"]);
        Assert.Equal(SessionMode.Review, s.Mode);
        Assert.Equal(["q1", "q9"], s.Items.Select(i => i.Question.Id).Order());
    }

    [Fact]
    public void Review_with_nothing_throws()
    {
        Assert.Throws<InvalidOperationException>(() => Factory().CreateReview(["nope"]));
    }

    [Fact]
    public void Same_seed_gives_same_exam()
    {
        var a = Factory(7).CreateMockExam("mock").Items.Select(i => i.Question.Id).ToList();
        var b = Factory(7).CreateMockExam("mock").Items.Select(i => i.Question.Id).ToList();
        Assert.Equal(a, b);
    }
}
