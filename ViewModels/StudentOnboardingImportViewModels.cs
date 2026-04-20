using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SmartELibrary.ViewModels;

public class StudentOnboardingUploadViewModel
{
    [Required]
    [Display(Name = "Excel File (.xlsx)")]
    public IFormFile? ExcelFile { get; set; }
}

public class StudentOnboardingImportResultViewModel
{
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class StudentOnboardingRowViewModel
{
    public int Id { get; set; }
    public string EnrollmentNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public bool IsRegistered { get; set; }
    public DateTime ImportedAtUtc { get; set; }
}

public class AdminStudentOnboardingPageViewModel
{
    public StudentOnboardingUploadViewModel Upload { get; set; } = new();
    public StudentOnboardingImportResultViewModel? LastImportResult { get; set; }
    public IReadOnlyList<StudentOnboardingRowViewModel> Rows { get; set; } = Array.Empty<StudentOnboardingRowViewModel>();
}
