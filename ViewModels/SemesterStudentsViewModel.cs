namespace SmartELibrary.ViewModels;

public class SemesterStudentsGroupViewModel
{
    public int SemesterId { get; set; }

    public string SemesterName { get; set; } = string.Empty;

    public List<SemesterStudentRowViewModel> Students { get; set; } = new();
}

public class SemesterStudentRowViewModel
{
    public int StudentId { get; set; }

    public int EnrollmentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string EnrollmentNumber { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? EnrolledAtUtc { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }
}
