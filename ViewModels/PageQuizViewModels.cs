namespace SmartELibrary.ViewModels;

public class PageQuizDetailsDto
{
    public int QuizId { get; set; }

    public List<PageQuizQuestionDto> Questions { get; set; } = new();
}

public class PageQuizQuestionDto
{
    public int QuestionId { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    public string OptionA { get; set; } = string.Empty;

    public string OptionB { get; set; } = string.Empty;

    public string OptionC { get; set; } = string.Empty;

    public string OptionD { get; set; } = string.Empty;
}

public class PageQuizAttemptViewModel
{
    public int PageId { get; set; }

    public int QuizId { get; set; }

    public Dictionary<int, string> Answers { get; set; } = new();

    public string? AntiCheatReason { get; set; }
}
