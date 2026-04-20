using SmartELibrary.Models;

namespace SmartELibrary.ViewModels;

public class PendingRegistrationViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? EnrollmentNumber { get; set; }
    public UserRole Role { get; set; }
}
