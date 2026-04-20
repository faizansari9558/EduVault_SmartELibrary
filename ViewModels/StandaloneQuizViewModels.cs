using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.ViewModels;

/// <summary>ViewModel for the standalone Create Quiz form.</summary>
public class StandaloneQuizViewModel
{
    [Required(ErrorMessage = "Quiz title is required.")]
    [StringLength(180, ErrorMessage = "Title must not exceed 180 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a subject.")]
    public int SubjectId { get; set; }

    public int SemesterId { get; set; }

    /// <summary>Time allowed in minutes (null = unlimited).</summary>
    [Range(1, 300, ErrorMessage = "Time limit must be between 1 and 300 minutes.")]
    public int? TimeLimitMinutes { get; set; }

    /// <summary>Local date/time the quiz becomes available (bound from form, converted to UTC in controller).</summary>
    public DateTime? AvailableFrom { get; set; }

    /// <summary>Local date/time the quiz closes (deadline). Null = no deadline.</summary>
    public DateTime? AvailableTo { get; set; }

    [MinLength(1, ErrorMessage = "Add at least one question.")]
    public List<StandaloneQuizQuestionInputViewModel> Questions { get; set; } = new()
    {
        new StandaloneQuizQuestionInputViewModel()
    };
}

public class StandaloneQuizQuestionInputViewModel
{
    [Required(ErrorMessage = "Question text is required.")]
    [StringLength(500)]
    public string QuestionText { get; set; } = string.Empty;

    [Required(ErrorMessage = "Option A is required.")]
    [StringLength(200)]
    public string OptionA { get; set; } = string.Empty;

    [Required(ErrorMessage = "Option B is required.")]
    [StringLength(200)]
    public string OptionB { get; set; } = string.Empty;

    [Required(ErrorMessage = "Option C is required.")]
    [StringLength(200)]
    public string OptionC { get; set; } = string.Empty;

    [Required(ErrorMessage = "Option D is required.")]
    [StringLength(200)]
    public string OptionD { get; set; } = string.Empty;

    [Required(ErrorMessage = "Correct option is required.")]
    public string CorrectOption { get; set; } = "A";
}

// ── Quiz Results Page ──────────────────────────────────────────────────────

public class QuizResultsPageViewModel
{
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public DateTime? AvailableFromUtc { get; set; }
    public DateTime? AvailableToUtc { get; set; }
    public List<QuizResultRowViewModel> Results { get; set; } = new();
}

public class QuizResultRowViewModel
{
    public string StudentName { get; set; } = string.Empty;
    public string EnrollmentNo { get; set; } = string.Empty;
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public decimal ScorePercent { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public bool IsAutoSubmitted { get; set; }
    public string? AntiCheatReason { get; set; }
    public DateTime? AntiCheatDetectedAtUtc { get; set; }
}
