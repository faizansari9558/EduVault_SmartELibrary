using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.ViewModels;

public class RegisterStudentViewModel
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string EnrollmentNumber { get; set; } = string.Empty;

    [Required, StringLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
