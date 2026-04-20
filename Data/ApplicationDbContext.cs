using Microsoft.EntityFrameworkCore;
using SmartELibrary.Models;

namespace SmartELibrary.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<DeletedUser> DeletedUsers => Set<DeletedUser>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialPage> MaterialPages => Set<MaterialPage>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizResult> QuizResults => Set<QuizResult>();
    public DbSet<ProgressTracking> ProgressTrackings => Set<ProgressTracking>();
    public DbSet<MaterialPageProgress> MaterialPageProgress => Set<MaterialPageProgress>();
    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
    public DbSet<SemesterResultPublish> SemesterResultPublishes => Set<SemesterResultPublish>();
    public DbSet<StudentOnboardingRecord> StudentOnboardingRecords => Set<StudentOnboardingRecord>();
    public DbSet<PromotionLog> PromotionLogs => Set<PromotionLog>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(x => x.PhoneNumber).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();

        modelBuilder.Entity<DeletedUser>()
            .HasIndex(x => x.OriginalUserId)
            .IsUnique();

        // Keep default column naming for compatibility with existing migrations/schema:
        // Users.FullName, Users.CreatedAtUtc

        modelBuilder.Entity<Admin>()
            .HasIndex(x => x.UserId)
            .IsUnique();

        modelBuilder.Entity<Teacher>()
            .HasIndex(x => x.UserId)
            .IsUnique();

        modelBuilder.Entity<Teacher>()
            .HasIndex(x => x.TeacherId)
            .IsUnique();

        modelBuilder.Entity<Student>()
            .HasIndex(x => x.UserId)
            .IsUnique();

        modelBuilder.Entity<Student>()
            .HasOne(x => x.CurrentSemester)
            .WithMany()
            .HasForeignKey(x => x.CurrentSemesterId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Student>()
            .Property(x => x.PromotionStatus)
            .HasConversion<int>()
            .IsRequired();

        modelBuilder.Entity<Subject>()
            .HasIndex(x => x.SubjectCode)
            .IsUnique();

        modelBuilder.Entity<TeacherSubject>()
            .HasIndex(x => x.SubjectId)
            .HasDatabaseName("UX_TeacherSubjects_SubjectId")
            .IsUnique();

        modelBuilder.Entity<StudentEnrollment>()
            .HasIndex(x => new { x.StudentId, x.SemesterId })
            .IsUnique();

        modelBuilder.Entity<StudentOnboardingRecord>()
            .HasIndex(x => x.EnrollmentNo)
            .IsUnique();

        modelBuilder.Entity<StudentEnrollment>()
            .Property(x => x.ApprovedAtUtc)
            .HasColumnName("ApprovedAt");

        modelBuilder.Entity<QuizResult>()
            .HasIndex(x => new { x.QuizId, x.StudentId });

        modelBuilder.Entity<QuizResult>()
            .Property(x => x.AntiCheatReason)
            .HasMaxLength(120);

        modelBuilder.Entity<MaterialPage>()
            .HasIndex(x => new { x.MaterialId, x.PageNumber })
            .IsUnique();

        modelBuilder.Entity<MaterialPageProgress>()
            .HasIndex(x => new { x.StudentId, x.MaterialPageId })
            .IsUnique();

        modelBuilder.Entity<Material>()
            .HasOne(x => x.Teacher)
            .WithMany(x => x.Materials)
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Admin>()
            .HasOne(x => x.User)
            .WithOne(x => x.Admin)
            .HasForeignKey<Admin>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Teacher>()
            .HasOne(x => x.User)
            .WithOne(x => x.Teacher)
            .HasForeignKey<Teacher>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Student>()
            .HasOne(x => x.User)
            .WithOne(x => x.Student)
            .HasForeignKey<Student>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MaterialPage>()
            .HasOne(x => x.Material)
            .WithMany(x => x.Pages)
            .HasForeignKey(x => x.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MaterialPageProgress>()
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaterialPageProgress>()
            .HasOne(x => x.MaterialPage)
            .WithMany(x => x.PageProgress)
            .HasForeignKey(x => x.MaterialPageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TeacherSubject>()
            .HasOne(x => x.Teacher)
            .WithMany(x => x.TeacherSubjects)
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StudentEnrollment>()
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StudentEnrollment>()
            .HasOne(x => x.ApprovedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.ApprovedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<QuizResult>()
            .HasOne(x => x.Student)
            .WithMany(x => x.QuizResults)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProgressTracking>()
            .HasOne(x => x.Student)
            .WithMany(x => x.ProgressTrackings)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SemesterResultPublish>()
            .HasIndex(x => x.SemesterId)
            .IsUnique();

        modelBuilder.Entity<SemesterResultPublish>()
            .HasOne(x => x.Semester)
            .WithMany()
            .HasForeignKey(x => x.SemesterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SemesterResultPublish>()
            .HasOne(x => x.PublishedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.PublishedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PromotionLog>()
            .HasOne(x => x.FromSemester)
            .WithMany()
            .HasForeignKey(x => x.FromSemesterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PromotionLog>()
            .HasOne(x => x.ToSemester)
            .WithMany()
            .HasForeignKey(x => x.ToSemesterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StudentOnboardingRecord>()
            .HasOne(x => x.Semester)
            .WithMany()
            .HasForeignKey(x => x.SemesterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StudentOnboardingRecord>()
            .HasOne(x => x.RegisteredUser)
            .WithMany()
            .HasForeignKey(x => x.RegisteredUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                FullName = "System Admin",
                PhoneNumber = "9999999999",
                PasswordHash = "pqp7RUbHAaT3rE1FB2yLYA==.DDvLiDdTGAeURgKOJzbSL8PIJIMsve3hWpUHfv9JOrk=",
                Role = UserRole.Admin,
                IsApproved = true,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
    }
}
