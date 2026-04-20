namespace SmartELibrary.Models;

public class TeacherSubject
{
    public int Id { get; set; }

    public int TeacherId { get; set; }
    public User? Teacher { get; set; }

    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
}
