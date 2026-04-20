using Microsoft.EntityFrameworkCore;
using SmartELibrary.Data;
using SmartELibrary.Models;

namespace SmartELibrary.Services;

public static class RoleTableBackfillService
{
    public static async Task BackfillAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .Select(x => new { x.Id, x.Role, x.EnrollmentNo })
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            return;
        }

        // Admins
        var existingAdminUserIds = await dbContext.Admins
            .AsNoTracking()
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        var adminUserIdsToInsert = users
            .Where(x => x.Role == UserRole.Admin)
            .Select(x => x.Id)
            .Where(id => !existingAdminUserIds.Contains(id))
            .ToList();

        foreach (var userId in adminUserIdsToInsert)
        {
            dbContext.Admins.Add(new Admin
            {
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        // Teachers
        var existingTeacherUserIds = await dbContext.Teachers
            .AsNoTracking()
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        var existingTeacherCodes = new HashSet<string>(
            await dbContext.Teachers.AsNoTracking().Select(x => x.TeacherId).ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);

        var teacherUsersToInsert = users
            .Where(x => x.Role == UserRole.Teacher)
            .Where(x => !existingTeacherUserIds.Contains(x.Id))
            .ToList();

        foreach (var teacherUser in teacherUsersToInsert)
        {
            var teacherCode = GenerateTeacherCode(teacherUser.Id, existingTeacherCodes);
            existingTeacherCodes.Add(teacherCode);

            dbContext.Teachers.Add(new Teacher
            {
                UserId = teacherUser.Id,
                TeacherId = teacherCode,
                AssignedAtUtc = DateTime.UtcNow
            });
        }

        // Students
        var existingStudentUserIds = await dbContext.Students
            .AsNoTracking()
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        var studentUsersToInsert = users
            .Where(x => x.Role == UserRole.Student)
            .Where(x => !existingStudentUserIds.Contains(x.Id))
            .ToList();

        foreach (var studentUser in studentUsersToInsert)
        {
            dbContext.Students.Add(new Student
            {
                UserId = studentUser.Id,
                EnrollmentNumber = (studentUser.EnrollmentNo ?? string.Empty).Trim(),
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static string GenerateTeacherCode(int userId, HashSet<string> existingTeacherCodes)
    {
        // Primary requested format: TID0001 based on UserId padding.
        var baseCode = $"TID{userId:D4}";
        var code = baseCode;

        if (!existingTeacherCodes.Contains(code))
        {
            return code;
        }

        // Extremely defensive fallback (shouldn't happen unless existing data is inconsistent).
        // Keeps the base format and appends a numeric suffix to preserve uniqueness.
        var suffix = 1;
        while (existingTeacherCodes.Contains(code))
        {
            code = $"{baseCode}{suffix:D2}";
            suffix++;
        }

        return code;
    }
}
