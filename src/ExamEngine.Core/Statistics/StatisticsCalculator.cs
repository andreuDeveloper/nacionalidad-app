using ExamEngine.Core.Models;
using ExamEngine.Core.Progress;
using ExamEngine.Core.Sessions;

namespace ExamEngine.Core.Statistics;

public sealed record CategoryStats(
    string CategoryId,
    int TotalQuestions,
    int Seen,
    int Mastered,
    int PendingReview,
    int Attempts,
    int CorrectAttempts)
{
    /// <summary>Porcentaje de aciertos sobre todas las respuestas dadas (0-1).</summary>
    public double Accuracy => Attempts == 0 ? 0 : (double)CorrectAttempts / Attempts;

    /// <summary>Parte del temario vista al menos una vez (0-1).</summary>
    public double Coverage => TotalQuestions == 0 ? 0 : (double)Seen / TotalQuestions;

    public double MasteryRatio => TotalQuestions == 0 ? 0 : (double)Mastered / TotalQuestions;
}

public sealed record OverallStats(
    int TotalQuestions,
    int Seen,
    int Mastered,
    int PendingReview,
    int Attempts,
    int CorrectAttempts,
    int MockExamsTaken,
    int MockExamsPassed,
    double? LastMockExamRatio,
    int StudyStreakDays,
    IReadOnlyList<CategoryStats> Categories)
{
    public double Accuracy => Attempts == 0 ? 0 : (double)CorrectAttempts / Attempts;
    public double Coverage => TotalQuestions == 0 ? 0 : (double)Seen / TotalQuestions;
    public double MasteryRatio => TotalQuestions == 0 ? 0 : (double)Mastered / TotalQuestions;
    public double MockPassRate => MockExamsTaken == 0 ? 0 : (double)MockExamsPassed / MockExamsTaken;

    /// <summary>
    /// Estimación sencilla de preparación (0-1): mezcla dominio del temario y nota de los simulacros.
    /// </summary>
    public double Readiness
    {
        get
        {
            var mastery = MasteryRatio;
            if (LastMockExamRatio is not { } mock) return mastery * 0.8;
            return Math.Clamp(0.5 * mastery + 0.5 * mock, 0, 1);
        }
    }
}

public static class StatisticsCalculator
{
    public static OverallStats Compute(
        QuestionBank bank,
        IReadOnlyList<AnswerRecord> answers,
        IReadOnlyList<SessionRecord> sessions,
        ProgressTracker tracker,
        DateOnly today)
    {
        // Solo cuentan preguntas que siguen en el banco (el banco se actualiza cada año).
        var validIds = bank.Questions.Select(q => q.Id).ToHashSet();
        var relevant = answers.Where(a => validIds.Contains(a.QuestionId)).ToList();
        var states = ProgressTracker.BuildStates(relevant);
        var pending = tracker.PendingReview(states.Values).ToHashSet();

        var categories = bank.Categories.OrderBy(c => c.Order).Select(c =>
        {
            var ids = bank.QuestionsIn(c.Id).Select(q => q.Id).ToHashSet();
            var catStates = states.Values.Where(s => ids.Contains(s.QuestionId)).ToList();
            var catAnswers = relevant.Where(a => ids.Contains(a.QuestionId)).ToList();
            return new CategoryStats(
                c.Id,
                ids.Count,
                catStates.Count,
                catStates.Count(tracker.IsMastered),
                catStates.Count(s => pending.Contains(s.QuestionId)),
                catAnswers.Count,
                catAnswers.Count(a => a.IsCorrect));
        }).ToList();

        var mocks = sessions.Where(s => s.Mode == SessionMode.MockExam && s.Total > 0)
                            .OrderBy(s => s.FinishedAt).ToList();

        return new OverallStats(
            bank.Questions.Count,
            states.Count,
            states.Values.Count(tracker.IsMastered),
            pending.Count,
            relevant.Count,
            relevant.Count(a => a.IsCorrect),
            mocks.Count,
            mocks.Count(m => m.Passed == true),
            mocks.Count > 0 ? (double)mocks[^1].Correct / mocks[^1].Total : null,
            StudyStreak(relevant.Select(a => a.AnsweredAt), today),
            categories);
    }

    /// <summary>Días seguidos con actividad terminando hoy o ayer (en hora local).</summary>
    public static int StudyStreak(IEnumerable<DateTimeOffset> activity, DateOnly today)
    {
        var days = activity.Select(a => DateOnly.FromDateTime(a.ToLocalTime().Date)).ToHashSet();
        var cursor = days.Contains(today) ? today : today.AddDays(-1);
        var streak = 0;
        while (days.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }
        return streak;
    }
}
