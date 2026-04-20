namespace SmartELibrary.Models;

public class MaterialPageProgress
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public User? Student { get; set; }

    public int MaterialPageId { get; set; }
    public MaterialPage? MaterialPage { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public double TimeSpentSeconds { get; set; }

    // Max scroll depth reached for this page (0-100).
    public double MaxScrollDepthPercent { get; set; }

    // Teacher/Admin analytics flag: true when first-time page completion time is under the engagement threshold.
    public bool LowEngagement { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}
