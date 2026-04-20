using SmartELibrary.Models;

namespace SmartELibrary.ViewModels;

public class UnifiedRegisterViewModel
{
    public string CurrentStep { get; set; } = "lookup";

    public string UserIdentifier { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;

    public int? UserId { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? EnrollmentNumber { get; set; }
    public string? TeacherId { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public string? DebugOtp { get; set; }
}