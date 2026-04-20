namespace SmartELibrary.ViewModels;

public class ReadMaterialPageViewModel
{
    public int MaterialId { get; set; }

    public string MaterialTitle { get; set; } = string.Empty;

    public int PageId { get; set; }

    public int PageNumber { get; set; }

    public int TotalPages { get; set; }

    public string PageTitle { get; set; } = string.Empty;

    public string HtmlContent { get; set; } = string.Empty;

    public bool HasMandatoryQuizAfterThisPage { get; set; }

    public bool QuizPassed { get; set; }

    public int? QuizId { get; set; }

    public bool HasPendingQuiz { get; set; }

    public double TimeSpentSeconds { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public bool IsPageCompleted { get; set; }

    public double ChapterCompletionPercent { get; set; }

    public bool IsFirstPage => PageNumber <= 1;

    public bool IsLastPage => PageNumber >= TotalPages;
}
