using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.ViewModels;

public class ForgotPasswordViewModel
{
    [Required]
    [Phone]
    [Display(Name = "Mobile Number")]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class VerifyForgotPasswordOtpViewModel
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    [Display(Name = "OTP")]
    public string OtpCode { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    [Required]
    [Phone]
    [Display(Name = "Mobile Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Previous Password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
