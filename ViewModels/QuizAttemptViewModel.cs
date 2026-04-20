using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.ViewModels;

public class QuizAttemptViewModel
{
    public int QuizId { get; set; }

    [Required]
    public Dictionary<int, string> Answers { get; set; } = new();

    public string? AntiCheatReason { get; set; }
}
