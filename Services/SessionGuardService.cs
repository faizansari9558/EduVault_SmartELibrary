using Microsoft.EntityFrameworkCore;
using SmartELibrary.Data;
using SmartELibrary.Models;

namespace SmartELibrary.Services;

public interface ISessionGuardService
{
    Task<bool> HasValidAuthenticatedSessionAsync(HttpContext context);
    Task LogoutAsync(HttpContext context);
}

public class SessionGuardService(ApplicationDbContext dbContext, IStudentSessionTracker studentSessionTracker) : ISessionGuardService
{
    public async Task<bool> HasValidAuthenticatedSessionAsync(HttpContext context)
    {
        var userId = context.Session.GetInt32("UserId");
        var roleValue = context.Session.GetString("Role");

        if (userId is null || string.IsNullOrWhiteSpace(roleValue))
        {
            return true;
        }

        if (!Enum.TryParse<UserRole>(roleValue, out var sessionRole))
        {
            return false;
        }

        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value);
        if (user is null)
        {
            return false;
        }

        if (user.Role != sessionRole)
        {
            return false;
        }

        if (user.Role != UserRole.Admin && !user.IsApproved)
        {
            return false;
        }

        return true;
    }

    public Task LogoutAsync(HttpContext context)
    {
        var userId = context.Session.GetInt32("UserId");
        var role = context.Session.GetString("Role");

        if (userId.HasValue && string.Equals(role, UserRole.Student.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            var sessionId = context.Session.GetString("StudentSessionId");
            studentSessionTracker.EndSession(userId.Value, sessionId);
        }

        context.Session.Clear();
        return Task.CompletedTask;
    }
}