namespace SmartELibrary.Models;

public class QuizResult
{
    public int Id { get; set; }

    public int QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    public int StudentId { get; set; }
    public User? Student { get; set; }

    public int CorrectAnswers { get; set; }

    public int TotalQuestions { get; set; }

    public decimal ScorePercent { get; set; }

    public bool IsAutoSubmitted { get; set; }

    public string? AntiCheatReason { get; set; }

    public DateTime? AntiCheatDetectedAtUtc { get; set; }

    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
}
