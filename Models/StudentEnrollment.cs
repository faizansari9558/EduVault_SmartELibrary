namespace SmartELibrary.Models;

public class StudentEnrollment
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public User? Student { get; set; }

    public int SemesterId { get; set; }
    public Semester? Semester { get; set; }

    public bool IsApproved { get; set; }

    public int? ApprovedByAdminId { get; set; }
    public Admin? ApprovedByAdmin { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }

    public DateTime EnrolledAtUtc { get; set; } = DateTime.UtcNow;
}
