namespace SmartELibrary.Models;

public class SemesterResultPublish
{
    public int Id { get; set; }

    public int SemesterId { get; set; }
    public Semester? Semester { get; set; }

    public bool IsPublished { get; set; }
    public DateTime? PublishedAtUtc { get; set; }

    public int? PublishedByAdminId { get; set; }
    public Admin? PublishedByAdmin { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
