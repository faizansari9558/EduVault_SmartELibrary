using System.ComponentModel.DataAnnotations;

namespace SmartELibrary.Models;

public class Subject
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string SubjectCode { get; set; } = string.Empty;

    public int SemesterId { get; set; }
    public Semester? Semester { get; set; }

    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
    public ICollection<Material> Materials { get; set; } = new List<Material>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
