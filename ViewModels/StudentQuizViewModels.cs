namespace SmartELibrary.ViewModels;

public class StudentQuizListViewModel
{
    public List<StudentQuizItemViewModel> Quizzes { get; set; } = new();
}

public class StudentQuizItemViewModel
{
    public int QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string SemesterName { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public DateTime? AvailableFromUtc { get; set; }
    public DateTime? AvailableToUtc { get; set; }

    // Attempt info
    public bool AlreadyAttempted { get; set; }
    public decimal? PreviousScore { get; set; }   // null if not attempted

    // Computed status
    public string Status { get; set; } = "Active"; // Active | Upcoming | Expired
}
