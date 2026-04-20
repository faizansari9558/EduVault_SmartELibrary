using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.ViewModels;

public class ChapterUploadViewModel
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    public int SubjectId { get; set; }

    public int SemesterId { get; set; }

    public int? TopicId { get; set; }

    public bool IsPublic { get; set; }

    [MinLength(2, ErrorMessage = "Add at least 2 pages.")]
    public List<ChapterPageInputViewModel> Pages { get; set; } =
    [
        new ChapterPageInputViewModel { PageTitle = "Page 1" },
        new ChapterPageInputViewModel { PageTitle = "Page 2" }
    ];
}

public class ChapterPageInputViewModel
{
    public int? PageId { get; set; }

    public int? QuizId { get; set; }

    [Required]
    public string PageTitle { get; set; } = string.Empty;

    [Required]
    public string HtmlContent { get; set; } = string.Empty;

    public string? QuizTitle { get; set; }

    public List<ChapterPageQuestionInputViewModel> Questions { get; set; } = [];
}

public class ChapterPageQuestionInputViewModel
{
    [Required]
    public string QuestionText { get; set; } = string.Empty;

    [Required]
    public string OptionA { get; set; } = string.Empty;

    [Required]
    public string OptionB { get; set; } = string.Empty;

    [Required]
    public string OptionC { get; set; } = string.Empty;

    [Required]
    public string OptionD { get; set; } = string.Empty;

    [Required]
    public string CorrectOption { get; set; } = "A";
}
