using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.Models;

public class User
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Email { get; set; }

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    // Legacy columns kept for backward-compatible migrations.
    // Authentication remains on Users, and these columns may still exist in existing databases.
    public bool IsPhoneVerified { get; set; }

    [StringLength(50)]
    public string? EnrollmentNo { get; set; }

    public bool IsApproved { get; set; }

    public bool IsFirstLogin { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
    public ICollection<Material> Materials { get; set; } = new List<Material>();
    public ICollection<QuizResult> QuizResults { get; set; } = new List<QuizResult>();
    public ICollection<ProgressTracking> ProgressTrackings { get; set; } = new List<ProgressTracking>();

    public Admin? Admin { get; set; }
    public Teacher? Teacher { get; set; }
    public Student? Student { get; set; }
}
