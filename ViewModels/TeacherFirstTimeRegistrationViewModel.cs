using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.ViewModels;

public class TeacherFirstTimeRegistrationViewModel
{
    [Required]
    [Display(Name = "Teacher ID or Phone")]
    [StringLength(30)]
    public string TeacherIdentifier { get; set; } = string.Empty;

    [Display(Name = "Teacher Name")]
    public string Name { get; set; } = string.Empty;

    public bool IsVerified { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [MinLength(8)]
    [Display(Name = "Set Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
