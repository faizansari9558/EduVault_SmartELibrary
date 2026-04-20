using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.Models;

public class Quiz
{
    public int Id { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public int? TopicId { get; set; }
    public Topic? Topic { get; set; }

    public int? MaterialId { get; set; }
    public Material? Material { get; set; }

    public int? MaterialPageId { get; set; }
    public MaterialPage? MaterialPage { get; set; }

    public int TeacherId { get; set; }
    public User? Teacher { get; set; }

    /// <summary>Time allowed for the student to complete the quiz (null = unlimited).</summary>
    public int? TimeLimitMinutes { get; set; }

    /// <summary>Earliest date/time the quiz becomes available to students (null = always available).</summary>
    public DateTime? AvailableFromUtc { get; set; }

    /// <summary>Last date/time students can attempt the quiz (null = no deadline).</summary>
    public DateTime? AvailableToUtc { get; set; }

    public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
    public ICollection<QuizResult> QuizResults { get; set; } = new List<QuizResult>();
}
