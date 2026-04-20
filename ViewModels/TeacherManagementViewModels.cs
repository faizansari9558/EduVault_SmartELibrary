using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SmartELibrary.ViewModels;

public class TeacherManualCreateViewModel
{
    [StringLength(20)]
    [Display(Name = "Teacher ID")]
    public string? TeacherId { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Code Name")]
    public string? CodeName { get; set; }

    [Required, EmailAddress, StringLength(150)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(15)]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;
}

public class TeacherUploadViewModel
{
    [Required]
    [Display(Name = "Teachers Excel (.xlsx)")]
    public IFormFile? ExcelFile { get; set; }
}

public class TeacherImportResultViewModel
{
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> GeneratedTemporaryPasswords { get; set; } = new();
}

public class TeacherRowViewModel
{
    public int UserId { get; set; }
    public string TeacherId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CodeName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsFirstLogin { get; set; }
}

public class TeacherManagementPageViewModel
{
    public TeacherManualCreateViewModel Manual { get; set; } = new();
    public TeacherUploadViewModel Upload { get; set; } = new();
    public TeacherImportResultViewModel? LastImportResult { get; set; }
    public IReadOnlyList<TeacherRowViewModel> Teachers { get; set; } = Array.Empty<TeacherRowViewModel>();
}

public class EditTeacherViewModel
{
    public int UserId { get; set; }

    [Required, StringLength(20)]
    [Display(Name = "Teacher ID")]
    public string TeacherId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(50)]
    [Display(Name = "Code Name")]
    public string CodeName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(15)]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; }
}
