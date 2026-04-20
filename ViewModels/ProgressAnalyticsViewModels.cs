namespace SmartELibrary.ViewModels;

public class ProgressAnalyticsRowViewModel
{
    public string EnrollmentNo { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public string Semester { get; set; } = string.Empty;

    public double ScreenTimeMinutes { get; set; }

    public string ScreenTimeHms { get; set; } = "00:00:00";

    public double QuizScorePercent { get; set; }

    public string QuizMarks { get; set; } = "0/0";

    public int QuizViolationCount { get; set; }

    public string? LatestQuizViolationReason { get; set; }

    public DateTime? LatestQuizViolationAtUtc { get; set; }

    public double CompletionPercent { get; set; }

    public double FinalProgressPercent { get; set; }

    public string Status { get; set; } = string.Empty;

    public string ProgressBarClass { get; set; } = "bg-secondary";
}

public class ProgressAnalyticsDashboardViewModel
{
    public List<ProgressAnalyticsRowViewModel> Rows { get; set; } = new();

    public double AverageScreenTimeMinutes { get; set; }

    public double AverageScreenTimePercentForFormula { get; set; }

    public double AverageQuizScorePercent { get; set; }

    public double AverageCompletionPercent { get; set; }
}
