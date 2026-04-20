using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.ViewModels;

public class StudentVerifyOtpViewModel
{
    [Required, StringLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(6, MinimumLength = 6)]
    public string OtpCode { get; set; } = string.Empty;
}
