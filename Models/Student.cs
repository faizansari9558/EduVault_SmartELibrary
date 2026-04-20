using System.ComponentModel.DataAnnotations;
using SmartELibrary.Enums;

namespace SmartELibrary.Models;

public class Student
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, StringLength(50)]
    public string EnrollmentNumber { get; set; } = string.Empty;

    public int? CurrentSemesterId { get; set; }
    public Semester? CurrentSemester { get; set; }

    public PromotionStatus PromotionStatus { get; set; } = PromotionStatus.Hold;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
