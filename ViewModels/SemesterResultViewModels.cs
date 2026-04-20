namespace SmartELibrary.ViewModels;

// ---- Used by both Admin semester list and Student published results list ----
public class SemesterResultRowViewModel
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public int ApprovedStudentCount { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}

public class SemesterResultIndexViewModel
{
    public List<SemesterResultRowViewModel> Semesters { get; set; } = new();
}

// ---- Admin: all students' result summaries for one semester ----
public class SemesterResultDetailViewModel
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public List<StudentResultSummaryRow> Students { get; set; } = new();
}

public class StudentResultSummaryRow
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string EnrollmentNumber { get; set; } = string.Empty;
    public double FinalResult { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
}

// ---- Full per-student result (used by Admin detail view and Student own result) ----
public class StudentSemesterResultViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string EnrollmentNumber { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public List<SubjectResultViewModel> Subjects { get; set; } = new();
    public double FinalResult { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
    public bool IsOwnResult { get; set; }
}

public class SubjectResultViewModel
{
    public string SubjectName { get; set; } = string.Empty;
    public List<MaterialResultRowViewModel> Materials { get; set; } = new();
    public double SubjectAverage { get; set; }
}

public class MaterialResultRowViewModel
{
    public string Title { get; set; } = string.Empty;
    public double CompletionPercent { get; set; }
    public double QuizScorePercent { get; set; }
    public double FinalProgress { get; set; }
}

// ---- Shared helper ----
public static class SemesterResultHelper
{
    public static (string Status, string StatusClass) GetStatus(double finalResult)
    {
        if (finalResult >= 80) return ("Excellent", "text-success");
        if (finalResult >= 60) return ("Good", "text-primary");
        if (finalResult >= 40) return ("Average", "text-warning");
        return ("Needs Improvement", "text-danger");
    }

    public static string GetProgressBarClass(double finalResult)
    {
        if (finalResult >= 80) return "bg-success";
        if (finalResult >= 60) return "bg-primary";
        if (finalResult >= 40) return "bg-warning";
        return "bg-danger";
    }
}
