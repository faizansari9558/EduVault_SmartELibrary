namespace SmartELibrary.ViewModels;

public class SemesterPromotionDashboardViewModel
{
    public int CurrentSemesterId { get; set; }

    public string CurrentSemesterName { get; set; } = string.Empty;

    public int? NextSemesterId { get; set; }

    public string NextSemesterName { get; set; } = string.Empty;

    public int TotalStudents { get; set; }

    public int StudentsToBePromoted { get; set; }

    public int StudentsOnHold { get; set; }

    public List<SemesterPromotionStudentRowViewModel> Students { get; set; } = new();
}

public class SemesterPromotionStudentRowViewModel
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public string EnrollmentNumber { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string PromotionStatus { get; set; } = string.Empty;

    public string CurrentSemesterName { get; set; } = string.Empty;
}