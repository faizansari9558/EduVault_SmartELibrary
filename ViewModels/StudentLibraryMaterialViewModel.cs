namespace SmartELibrary.ViewModels;

public class StudentLibraryMaterialViewModel
{
    public int MaterialId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public int TotalPages { get; set; }

    public int ResumePageNumber { get; set; } = 1;

    public int CompletedPages { get; set; }

    public bool HasProgress { get; set; }
}