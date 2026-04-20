using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.Models;

public class StudentOnboardingRecord
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string EnrollmentNo { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(150), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(15)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    public int SemesterId { get; set; }
    public Semester? Semester { get; set; }

    public bool IsRegistered { get; set; }

    public int? RegisteredUserId { get; set; }
    public User? RegisteredUser { get; set; }

    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RegisteredAtUtc { get; set; }
}