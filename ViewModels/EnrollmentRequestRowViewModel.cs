namespace SmartELibrary.ViewModels;

public class EnrollmentRequestRowViewModel
{
    public int EnrollmentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public string EnrollmentNumber { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime EnrolledAtUtc { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }
}
