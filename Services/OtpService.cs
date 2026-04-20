using Microsoft.EntityFrameworkCore;
using SmartELibrary.Data;
using SmartELibrary.Models;
using System.Collections.Concurrent;

namespace SmartELibrary.Services;

public interface IOtpService
{
    Task<string> GenerateOtpAsync(string phoneNumber);
    Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode);
    Task SendOtpSmsPlaceholderAsync(string phoneNumber, string otpCode);
    Task<string> GenerateEmailOtpAsync(string email);
    Task<bool> VerifyEmailOtpAsync(string email, string otpCode);
    Task SendOtpEmailPlaceholderAsync(string email, string otpCode);
}

public class OtpService(ApplicationDbContext dbContext) : IOtpService
{
    private static readonly ConcurrentDictionary<string, OtpMemoryEntry> EmailOtps = new(StringComparer.OrdinalIgnoreCase);

    private sealed record OtpMemoryEntry(string OtpCode, DateTime ExpiresAtUtc, bool IsUsed);

    public async Task<string> GenerateOtpAsync(string phoneNumber)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();

        var entity = new OtpVerification
        {
            PhoneNumber = phoneNumber,
            OtpCode = code,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
        };

        dbContext.OtpVerifications.Add(entity);
        await dbContext.SaveChangesAsync();

        return code;
    }

    public async Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode)
    {
        var otp = await dbContext.OtpVerifications
            .Where(x => x.PhoneNumber == phoneNumber && !x.IsUsed && x.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (otp is null || otp.OtpCode != otpCode)
        {
            return false;
        }

        otp.IsUsed = true;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public Task SendOtpSmsPlaceholderAsync(string phoneNumber, string otpCode)
    {
        return Task.CompletedTask;
    }

    public Task<string> GenerateEmailOtpAsync(string email)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        var normalizedEmail = email.Trim().ToLowerInvariant();

        EmailOtps[normalizedEmail] = new OtpMemoryEntry(code, DateTime.UtcNow.AddMinutes(5), false);
        return Task.FromResult(code);
    }

    public Task<bool> VerifyEmailOtpAsync(string email, string otpCode)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!EmailOtps.TryGetValue(normalizedEmail, out var entry))
        {
            return Task.FromResult(false);
        }

        if (entry.IsUsed || entry.ExpiresAtUtc <= DateTime.UtcNow || entry.OtpCode != otpCode)
        {
            return Task.FromResult(false);
        }

        EmailOtps[normalizedEmail] = entry with { IsUsed = true };
        return Task.FromResult(true);
    }

    public Task SendOtpEmailPlaceholderAsync(string email, string otpCode)
    {
        return Task.CompletedTask;
    }
}
