using Microsoft.EntityFrameworkCore;
using SmartELibrary.Data;

namespace SmartELibrary.Services;

public interface ITeacherCodeGenerator
{
    Task<string> GenerateNextAsync();
}

public class TeacherCodeGenerator(ApplicationDbContext dbContext) : ITeacherCodeGenerator
{
    private static readonly object SyncLock = new();

    public Task<string> GenerateNextAsync()
    {
        lock (SyncLock)
        {
            return GenerateNextInternalAsync();
        }
    }

    private async Task<string> GenerateNextInternalAsync()
    {
        var latestCode = await dbContext.Teachers
            .OrderByDescending(x => x.Id)
            .Select(x => x.TeacherId)
            .FirstOrDefaultAsync();

        var nextNumber = 1;
        if (!string.IsNullOrWhiteSpace(latestCode) && latestCode.StartsWith("TID", StringComparison.OrdinalIgnoreCase))
        {
            var numeric = latestCode[3..];
            if (int.TryParse(numeric, out var parsed))
            {
                nextNumber = parsed + 1;
            }
        }

        return $"TID{nextNumber:0000}";
    }
}
