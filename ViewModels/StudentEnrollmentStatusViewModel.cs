namespace SmartELibrary.ViewModels;

public class StudentEnrollmentStatusViewModel
{
    public string SemesterName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime EnrolledAtUtc { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }
}
