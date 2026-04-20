namespace SmartELibrary.Models;

public class PromotionLog
{
    public int Id { get; set; }

    public int FromSemesterId { get; set; }
    public Semester? FromSemester { get; set; }

    public int ToSemesterId { get; set; }
    public Semester? ToSemester { get; set; }

    public int TotalPromoted { get; set; }

    public int TotalHeld { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}