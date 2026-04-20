using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.ViewModels;

public class StudentControlledRegistrationViewModel
{
    [Required]
    [Display(Name = "Enrollment Number")]
    public string EnrollmentNo { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    [Display(Name = "Student Name")]
    public string Name { get; set; } = string.Empty;

    public bool IsVerified { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Create Password")]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
