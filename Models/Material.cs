using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.Models;

public class Material
{
    public int Id { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [StringLength(400)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public MaterialType MaterialType { get; set; }

    [Required]
    public string FilePathOrUrl { get; set; } = string.Empty;

    public bool IsPublic { get; set; }

    public int SemesterId { get; set; }
    public Semester? Semester { get; set; }

    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public int? TopicId { get; set; }
    public Topic? Topic { get; set; }

    public int TeacherId { get; set; }
    public User? Teacher { get; set; }

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<MaterialPage> Pages { get; set; } = new List<MaterialPage>();
}
