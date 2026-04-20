namespace SmartELibrary.Services;

public interface IProgressService
{
    double Calculate(double completionPercent, double quizParticipationPercent);
    bool IsLowEngagement(double minCompletedPageSeconds);

    ProgressBreakdown CalculateBreakdown(
        double totalActiveReadingSeconds,
        double effectiveActiveReadingSecondsForFormula,
        int totalPages,
        int completedPages,
        double averageQuizScorePercent,
        double averageScrollDepthPercent,
        double idealMinutesPerPage);

    string GetProgressStatus(double finalProgressPercentage);
}

public record ProgressBreakdown(
    double ScreenTimeMinutes,
    double IdealReadingReferenceMinutes,
    double ScrollDepthPercent,
    double QuizScorePercent,
    double CompletionPercent,
    double ScreenTimePercentage,
    double FinalProgressPercent);

public class ProgressService : IProgressService
{
    private const double LowEngagementSecondsThreshold = 30;
    private const double ScreenTimeWeight = 0.50d;
    private const double QuizScoreWeight = 0.40d;
    private const double CompletionWeight = 0.10d;

    public double Calculate(double completionPercent, double quizParticipationPercent)
    {
        // Progress is based on learning completion + quiz participation.
        // Time spent remains tracked for analytics but does not block navigation.
        var score = (completionPercent * 0.7) + (quizParticipationPercent * 0.3);
        return Math.Round(Math.Clamp(score, 0, 100), 2);
    }

    public ProgressBreakdown CalculateBreakdown(
        double totalActiveReadingSeconds,
        double effectiveActiveReadingSecondsForFormula,
        int totalPages,
        int completedPages,
        double averageQuizScorePercent,
        double averageScrollDepthPercent,
        double idealMinutesPerPage)
    {
        var idealSecondsPerPage = Math.Max(0d, idealMinutesPerPage * 60d);
        var idealTotalSeconds = totalPages <= 0 ? 0d : totalPages * idealSecondsPerPage;

        var completionPercent = totalPages <= 0 ? 0d : (double)completedPages / totalPages * 100d;

        var screenTimePercentage = idealTotalSeconds <= 0d
            ? 0d
            : Math.Min((effectiveActiveReadingSecondsForFormula / idealTotalSeconds) * 100d, 100d);

        averageQuizScorePercent = Math.Clamp(averageQuizScorePercent, 0d, 100d);
        completionPercent = Math.Clamp(completionPercent, 0d, 100d);

        var finalProgress = (screenTimePercentage * ScreenTimeWeight)
                    + (averageQuizScorePercent * QuizScoreWeight)
                    + (completionPercent * CompletionWeight);

        finalProgress = Math.Round(Math.Clamp(finalProgress, 0d, 100d), 2);

        return new ProgressBreakdown(
            ScreenTimeMinutes: Math.Round(Math.Max(0d, totalActiveReadingSeconds) / 60d, 2),
            IdealReadingReferenceMinutes: idealMinutesPerPage,
            ScrollDepthPercent: Math.Round(Math.Clamp(averageScrollDepthPercent, 0d, 100d), 2),
            QuizScorePercent: Math.Round(averageQuizScorePercent, 2),
            CompletionPercent: Math.Round(completionPercent, 2),
            ScreenTimePercentage: Math.Round(Math.Clamp(screenTimePercentage, 0d, 100d), 2),
            FinalProgressPercent: finalProgress);
    }

    public string GetProgressStatus(double finalProgressPercentage)
    {
        var p = Math.Round(Math.Clamp(finalProgressPercentage, 0d, 100d), 2);

        return p switch
        {
            < 20d => "Skimmer",
            < 40d => "NeedsImprovement",
            < 60d => "Learning",
            < 80d => "Progressing",
            < 90d => "ActiveLearner",
            _ => "Mastered"
        };
    }

    public bool IsLowEngagement(double minCompletedPageSeconds)
    {
        // Extremely low reading time indicates skimming.
        return minCompletedPageSeconds > 0 && minCompletedPageSeconds < LowEngagementSecondsThreshold;
    }
}
