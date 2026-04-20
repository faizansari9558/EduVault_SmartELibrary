using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.ViewModels;

public class UnifiedVerifiedRegistrationViewModel
{
    [Required]
    [Display(Name = "User Id")]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "OTP Code")]
    [Required(ErrorMessage = "OTP code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be exactly 6 digits")]
    public string OtpCode { get; set; } = string.Empty;

    [Display(Name = "Verification Step")]
    public int Step { get; set; } = 1;

    // Verified display data
    [Display(Name = "Name")]
    public string VerifiedName { get; set; } = string.Empty;

    public bool IsVerified { get; set; }

    public bool IsOtpSent { get; set; }

    public bool IsOtpVerified { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Create Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // Student/Teacher details for display on Step 3
    public string? StudentEnrollmentNo { get; set; }
    public string? StudentPhone { get; set; }
    public string? TeacherTeacherId { get; set; }
    public string? TeacherPhone { get; set; }
    public string? UserRole { get; set; }
}
