namespace SmartELibrary.Models;

public class ProgressTracking
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public User? Student { get; set; }

    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public int? TopicId { get; set; }
    public Topic? Topic { get; set; }

    public int? MaterialId { get; set; }
    public Material? Material { get; set; }

    public double ScreenTimeSeconds { get; set; }
    public double ScrollDepthPercent { get; set; }
    public double CompletionPercent { get; set; }
    public double QuizScorePercent { get; set; }

    public int QuizCorrectAnswers { get; set; }

    public int QuizTotalQuestions { get; set; }

    public double ProgressPercent { get; set; }

    public bool IsLowEngagementAlert { get; set; }

    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}
