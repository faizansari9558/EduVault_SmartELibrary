using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.Models;

public class DeletedUser
{
    public int Id { get; set; }

    public int OriginalUserId { get; set; }

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    [StringLength(50)]
    public string? EnrollmentNumber { get; set; }

    public bool IsApproved { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime DeletedAtUtc { get; set; } = DateTime.UtcNow;

    public int? DeletedByAdminUserId { get; set; }
}
