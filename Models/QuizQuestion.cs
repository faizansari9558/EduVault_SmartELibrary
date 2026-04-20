using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.Models;

public class QuizQuestion
{
    public int Id { get; set; }

    public int QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    [Required, StringLength(500)]
    public string QuestionText { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string OptionA { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string OptionB { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string OptionC { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string OptionD { get; set; } = string.Empty;

    [Required, StringLength(1)]
    public string CorrectOption { get; set; } = "A";
}
