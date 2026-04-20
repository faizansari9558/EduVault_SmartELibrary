using System.ComponentModel.DataAnnotations;
using SmartELibrary.Models;

namespace SmartELibrary.ViewModels;

public class EditUserViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(50)]
    public string? EnrollmentNo { get; set; }

    [Required]
    public UserRole Role { get; set; }

    public bool IsApproved { get; set; }
}
