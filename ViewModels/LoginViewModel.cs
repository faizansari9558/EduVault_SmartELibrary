using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.ViewModels;

public class LoginViewModel
{
    [Required, StringLength(50)]
    [Display(Name = "Phone Number / Teacher ID / Enrollment No")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
