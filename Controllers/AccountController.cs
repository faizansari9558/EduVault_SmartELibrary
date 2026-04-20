using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartELibrary.Data;
using SmartELibrary.Models;
using SmartELibrary.Services;
using SmartELibrary.ViewModels;
using System.Text.Json;

namespace SmartELibrary.Controllers;

[Route("Account")]
public class AccountController(ApplicationDbContext dbContext, IOtpService otpService) : Controller
{
    private const string PendingRegistrationActivationKey = "PendingRegistrationActivation";

    private sealed class PendingRegistrationActivationSession
    {
        public int UserId { get; set; }
        public string UserIdentifier { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool OtpVerified { get; set; }
    }

    [HttpGet("Register")]
    public async Task<IActionResult> Register(string? role = null)
    {
        var pendingSession = GetPendingActivationSession();
        if (pendingSession is null)
        {
            return View(new UnifiedRegisterViewModel());
        }

        var user = await FindUserByIdentifierAsync(pendingSession.UserIdentifier);
        if (user is null || user.Id != pendingSession.UserId)
        {
            ClearPendingActivationSession();
            return View(new UnifiedRegisterViewModel());
        }

        var model = await BuildRegisterModelFromUserAsync(user, pendingSession.UserIdentifier, pendingSession.Email);
        model.CurrentStep = pendingSession.OtpVerified ? "set-password" : "verify-otp";
        return View(model);
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register(UnifiedRegisterViewModel model)
    {
        var actionType = (Request.Form["actionType"].ToString() ?? string.Empty).Trim().ToLowerInvariant();
        return actionType switch
        {
            "request-otp" => await HandleRequestOtpAsync(model),
            "verify-otp" => await HandleVerifyOtpAsync(model),
            "set-password" => await HandleSetPasswordAsync(model),
            _ => View(new UnifiedRegisterViewModel())
        };
    }

    [HttpGet("RegisterTeacher")]
    public IActionResult RegisterTeacher() => RedirectToAction(nameof(Register));

    [HttpPost("RegisterTeacher")]
    public IActionResult RegisterTeacher(RegisterTeacherViewModel model)
    {
        TempData["Error"] = "Use the unified Register page with User ID and Email verification.";
        return RedirectToAction(nameof(Register));
    }

    [HttpGet("RegisterStudent")]
    public IActionResult RegisterStudent() => RedirectToAction(nameof(Register));

    [HttpPost("RegisterStudent")]
    public IActionResult RegisterStudent(RegisterStudentViewModel model)
    {
        TempData["Error"] = "Use the unified Register page with User ID and Email verification.";
        return RedirectToAction(nameof(Register));
    }

    private async Task<IActionResult> HandleRequestOtpAsync(UnifiedRegisterViewModel model)
    {
        var identifier = model.UserIdentifier.Trim();
        var normalizedEmail = model.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(identifier))
        {
            ModelState.AddModelError(nameof(model.UserIdentifier), "User ID is required.");
            model.CurrentStep = "lookup";
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            ModelState.AddModelError(nameof(model.Email), "Email is required.");
            model.CurrentStep = "lookup";
            return View(model);
        }

        var user = await FindUserByIdentifierAsync(identifier);
        if (user is null || (user.Role != UserRole.Student && user.Role != UserRole.Teacher))
        {
            ModelState.AddModelError(string.Empty, "No Student or Teacher account found for this User ID.");
            model.CurrentStep = "lookup";
            return View(model);
        }

        if (!string.Equals(user.Email?.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Email), "Email does not match the preloaded account.");
            model.CurrentStep = "lookup";
            return View(model);
        }

        if (!user.IsFirstLogin)
        {
            ModelState.AddModelError(string.Empty, "This account is already registered. Please login.");
            model.CurrentStep = "lookup";
            return View(model);
        }

        var otp = await otpService.GenerateEmailOtpAsync(user.Email ?? normalizedEmail);
        await otpService.SendOtpEmailPlaceholderAsync(user.Email ?? normalizedEmail, otp);

        SetPendingActivationSession(new PendingRegistrationActivationSession
        {
            UserId = user.Id,
            UserIdentifier = identifier,
            Email = normalizedEmail,
            PhoneNumber = user.PhoneNumber,
            OtpVerified = false
        });

        var vm = await BuildRegisterModelFromUserAsync(user, identifier, normalizedEmail);
        vm.CurrentStep = "verify-otp";
        vm.DebugOtp = otp;
        TempData["Success"] = "OTP sent to your registered email address.";
        return View(vm);
    }

    private async Task<IActionResult> HandleVerifyOtpAsync(UnifiedRegisterViewModel model)
    {
        var pendingSession = GetPendingActivationSession();
        if (pendingSession is null)
        {
            TempData["Error"] = "Registration session expired. Start again.";
            return RedirectToAction(nameof(Register));
        }

        if (string.IsNullOrWhiteSpace(model.OtpCode))
        {
            ModelState.AddModelError(nameof(model.OtpCode), "OTP is required.");
            var retryVm = await BuildRegisterModelFromPendingSessionAsync(pendingSession);
            retryVm.CurrentStep = "verify-otp";
            return View(retryVm);
        }

        var isValid = await otpService.VerifyEmailOtpAsync(pendingSession.Email, model.OtpCode.Trim());
        if (!isValid)
        {
            ModelState.AddModelError(nameof(model.OtpCode), "Invalid or expired OTP.");
            var retryVm = await BuildRegisterModelFromPendingSessionAsync(pendingSession);
            retryVm.CurrentStep = "verify-otp";
            return View(retryVm);
        }

        pendingSession.OtpVerified = true;
        SetPendingActivationSession(pendingSession);

        var successVm = await BuildRegisterModelFromPendingSessionAsync(pendingSession);
        successVm.CurrentStep = "set-password";
        TempData["Success"] = "Account verified. Set your password to complete registration.";
        return View(successVm);
    }

    private async Task<IActionResult> HandleSetPasswordAsync(UnifiedRegisterViewModel model)
    {
        var pendingSession = GetPendingActivationSession();
        if (pendingSession is null || !pendingSession.OtpVerified)
        {
            TempData["Error"] = "Complete OTP verification first.";
            return RedirectToAction(nameof(Register));
        }

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Password is required.");
        }

        if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
        {
            ModelState.AddModelError(nameof(model.ConfirmPassword), "Confirm Password is required.");
        }

        if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.ConfirmPassword), "Passwords do not match.");
        }

        var user = await dbContext.Users
            .Include(x => x.Student)
            .Include(x => x.Teacher)
            .FirstOrDefaultAsync(x => x.Id == pendingSession.UserId);
        if (user is null)
        {
            ClearPendingActivationSession();
            TempData["Error"] = "Account not found. Contact admin.";
            return RedirectToAction(nameof(Register));
        }

        if (!ModelState.IsValid)
        {
            var retryVm = await BuildRegisterModelFromUserAsync(user, pendingSession.UserIdentifier, pendingSession.Email);
            retryVm.CurrentStep = "set-password";
            return View(retryVm);
        }

        if (!user.IsFirstLogin)
        {
            ClearPendingActivationSession();
            TempData["Error"] = "This account is already registered. Please login.";
            return RedirectToAction("Login", "Auth");
        }

        user.PasswordHash = PasswordService.HashPassword(model.Password.Trim());
        user.IsFirstLogin = false;
        user.IsPhoneVerified = true;
        await dbContext.SaveChangesAsync();

        ClearPendingActivationSession();

        TempData["Success"] = "Registration completed successfully. Please login.";
        return RedirectToAction("Login", "Auth");
    }

    private async Task<UnifiedRegisterViewModel> BuildRegisterModelFromPendingSessionAsync(PendingRegistrationActivationSession pendingSession)
    {
        var user = await dbContext.Users
            .Include(x => x.Student)
            .Include(x => x.Teacher)
            .FirstOrDefaultAsync(x => x.Id == pendingSession.UserId);

        if (user is null)
        {
            return new UnifiedRegisterViewModel
            {
                CurrentStep = "lookup",
                UserIdentifier = pendingSession.UserIdentifier,
                Email = pendingSession.Email
            };
        }

        return await BuildRegisterModelFromUserAsync(user, pendingSession.UserIdentifier, pendingSession.Email);
    }

    private async Task<UnifiedRegisterViewModel> BuildRegisterModelFromUserAsync(User user, string identifier, string email)
    {
        DateTime? dateOfBirth = null;
        string? enrollmentNumber = null;
        string? teacherId = null;

        if (user.Role == UserRole.Student)
        {
            enrollmentNumber = user.Student?.EnrollmentNumber ?? user.EnrollmentNo;
            if (!string.IsNullOrWhiteSpace(enrollmentNumber))
            {
                dateOfBirth = await dbContext.StudentOnboardingRecords
                    .Where(x => x.EnrollmentNo == enrollmentNumber)
                    .Select(x => (DateTime?)x.DateOfBirth)
                    .FirstOrDefaultAsync();
            }
        }
        else if (user.Role == UserRole.Teacher)
        {
            teacherId = user.Teacher?.TeacherId;
        }

        return new UnifiedRegisterViewModel
        {
            CurrentStep = "lookup",
            UserId = user.Id,
            UserIdentifier = identifier,
            Email = email,
            Role = user.Role,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            EnrollmentNumber = enrollmentNumber,
            TeacherId = teacherId,
            DateOfBirth = dateOfBirth
        };
    }

    private void SetPendingActivationSession(PendingRegistrationActivationSession pendingSession)
    {
        HttpContext.Session.SetString(PendingRegistrationActivationKey, JsonSerializer.Serialize(pendingSession));
    }

    private PendingRegistrationActivationSession? GetPendingActivationSession()
    {
        var pendingJson = HttpContext.Session.GetString(PendingRegistrationActivationKey);
        if (string.IsNullOrWhiteSpace(pendingJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<PendingRegistrationActivationSession>(pendingJson);
    }

    private void ClearPendingActivationSession()
    {
        HttpContext.Session.Remove(PendingRegistrationActivationKey);
    }

    private async Task<User?> FindUserByIdentifierAsync(string identifier)
    {
        var normalized = identifier.Trim();
        return await dbContext.Users
            .Include(x => x.Teacher)
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x =>
                x.PhoneNumber == normalized
                || x.EnrollmentNo == normalized
                || (x.Teacher != null && x.Teacher.TeacherId == normalized)
                || (x.Student != null && x.Student.EnrollmentNumber == normalized));
    }
}
