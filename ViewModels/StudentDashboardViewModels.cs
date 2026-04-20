namespace SmartELibrary.ViewModels;

public class StudentDashboardViewModel
{
    public List<StudentEnrolledSubjectRowViewModel> Subjects { get; set; } = new();

    /// <summary>File materials grouped by subject (PDF, PPT, Image, ExternalLink).</summary>
    public List<StudentSubjectFilesViewModel> SubjectFiles { get; set; } = new();
}

public class StudentEnrolledSubjectRowViewModel
{
    public string SemesterName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
}

public class StudentSubjectFilesViewModel
{
    public string SemesterName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public List<StudentFileMaterialViewModel> Files { get; set; } = new();
}

public class StudentFileMaterialViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FilePathOrUrl { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }
}
