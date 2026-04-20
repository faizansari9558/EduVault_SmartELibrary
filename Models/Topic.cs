using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.Models;

public class Topic
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public int SequenceOrder { get; set; }

    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public ICollection<Material> Materials { get; set; } = new List<Material>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
