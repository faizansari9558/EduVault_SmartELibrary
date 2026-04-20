using Microsoft.EntityFrameworkCore;
using SmartELibrary.Data;
using SmartELibrary.Models;

namespace SmartELibrary.Services;

public static class AdminSeedService
{
    public static readonly string[] DefaultSemesterNames =
    [
        "Semester - 1",
        "Semester - 2",
        "Semester - 3",
        "Semester - 4",
        "Semester - 5",
        "Semester - 6",
        "Semester - 7",
        "Semester - 8"
    ];

    public static async Task EnsureAdminExistsAsync(ApplicationDbContext dbContext, string phoneNumber, string password, string fullName = "System Admin")
    {
        phoneNumber = (phoneNumber ?? string.Empty).Trim();
        password = password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var admin = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        if (admin is not null)
        {
            var existingAdmin = await dbContext.Admins.FirstOrDefaultAsync(x => x.UserId == admin.Id);
            if (existingAdmin is null)
            {
                dbContext.Admins.Add(new Admin { UserId = admin.Id, CreatedAtUtc = DateTime.UtcNow });
                await dbContext.SaveChangesAsync();
            }
            return;
        }

        var user = new User
        {
            FullName = fullName,
            PhoneNumber = phoneNumber,
            PasswordHash = PasswordService.HashPassword(password),
            Role = UserRole.Admin,
            IsApproved = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        dbContext.Admins.Add(new Admin
        {
            UserId = user.Id,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    public static async Task EnsureDefaultSemestersAsync(ApplicationDbContext dbContext)
    {
        var existingNames = await dbContext.Semesters
            .Select(x => x.Name)
            .ToListAsync();

        var existing = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

        var added = false;
        foreach (var name in DefaultSemesterNames)
        {
            if (existing.Contains(name))
            {
                continue;
            }

            dbContext.Semesters.Add(new Semester
            {
                Name = name,
                IsActive = string.Equals(name, "Semester - 1", StringComparison.OrdinalIgnoreCase)
            });
            added = true;
        }

        if (added)
        {
            await dbContext.SaveChangesAsync();
        }
    }
}
