using Microsoft.EntityFrameworkCore;
using SmartELibrary.Data;

namespace SmartELibrary.Services;

public static class DatabaseWipeService
{
    // Keeps auth/identity tables intact (Users/Admins/Teachers/Students) and preserves __EFMigrationsHistory.
    public static async Task WipeUserGeneratedContentAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        // Order matters when using DELETE; TRUNCATE with FK checks off is simpler.
        // We still do a deterministic order for clarity.
        var tablesToWipe = new[]
        {
            "MaterialPageProgress",
            "QuizResults",
            "QuizQuestions",
            "Quizzes",
            "ProgressTrackings",
            "MaterialPages",
            "Materials",
            "StudentEnrollments",
            "TeacherSubjects",
            "OtpVerifications"
        };

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;", cancellationToken);

            foreach (var table in tablesToWipe)
            {
                // Prefer TRUNCATE (fast + resets identity); fallback to DELETE if TRUNCATE isn't allowed.
                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE `" + table + "`;", cancellationToken);
                }
                catch
                {
                    await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM `" + table + "`;", cancellationToken);
                }
            }

            await dbContext.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;", cancellationToken);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    // Wipes *all* application data (keeps schema + __EFMigrationsHistory).
    public static async Task WipeAllApplicationDataAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var tablesToWipe = new[]
        {
            "MaterialPageProgress",
            "QuizResults",
            "QuizQuestions",
            "Quizzes",
            "ProgressTrackings",
            "StudentEnrollments",
            "TeacherSubjects",
            "MaterialPages",
            "Materials",
            "Topics",
            "Subjects",
            "Semesters",
            "Students",
            "Teachers",
            "Admins",
            "OtpVerifications",
            "Users"
        };

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;", cancellationToken);

            foreach (var table in tablesToWipe)
            {
                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE `" + table + "`;", cancellationToken);
                }
                catch
                {
                    await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM `" + table + "`;", cancellationToken);
                }
            }

            await dbContext.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;", cancellationToken);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}
