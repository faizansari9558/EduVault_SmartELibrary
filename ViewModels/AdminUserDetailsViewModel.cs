namespace SmartELibrary.ViewModels;

public class AdminUserRowViewModel
{
    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsApproved { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? TeacherId { get; set; }

    public int? AssignedSubjectCount { get; set; }

    public string? EnrollmentNumber { get; set; }

    public string? CurrentSemesterName { get; set; }

    public List<StudentEnrollmentSummaryViewModel> Enrollments { get; set; } = new();
}

public class StudentEnrollmentSummaryViewModel
{
    public string SemesterName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime EnrolledAtUtc { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }
}
