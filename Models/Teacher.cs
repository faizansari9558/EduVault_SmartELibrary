using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.Models;

public class Teacher
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, StringLength(20)]
    public string TeacherId { get; set; } = string.Empty;

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}
