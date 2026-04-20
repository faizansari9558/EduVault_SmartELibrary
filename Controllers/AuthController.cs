using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartELibrary.Data;
using SmartELibrary.Models;
using SmartELibrary.Services;
using SmartELibrary.ViewModels;
using System.Text.Json;

namespace SmartELibrary.Controllers;

public class AuthController(ApplicationDbContext dbContext, IOtpService otpService, ITeacherCodeGenerator teacherCodeGenerator) : Controller
{
    private const string StudentSessionKey = "StudentSessionId";
    private const string PendingStudentPhoneKey = "PendingStudentPhone";
    private const string PendingRegistrationKey = "PendingRegistration";

    [HttpGet]
    public IActionResult VerifyOtp()
    {
        ViewBag.PhoneNumber = TempData["PhoneNumber"]?.ToString();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> VerifyOtp(string phoneNumber, string otpCode)
    {
        var isValid = await otpService.VerifyOtpAsync(phoneNumber, otpCode);
        if (!isValid)
        {
            ModelState.AddModelError(string.Empty, "Invalid or expired OTP.");
            ViewBag.PhoneNumber = phoneNumber;
            return View();
        }

        var pendingJson = HttpContext.Session.GetString(PendingRegistrationKey);
        if (string.IsNullOrWhiteSpace(pendingJson))
        {
            TempData["Success"] = "Phone verified.";
            return RedirectToAction("Index", "Home");
        }

        var pending = JsonSerializer.Deserialize<PendingRegistrationViewModel>(pendingJson);
        if (pending is null || !string.Equals(pending.PhoneNumber, phoneNumber, StringComparison.Ordinal))
        {
            TempData["Error"] = "No pending registration found for this phone number.";
            return RedirectToAction("Index", "Home");
        }

        var existingUser = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == pending.PhoneNumber);
        if (existingUser is not null)
        {
            TempData["Error"] = "Phone number already registered.";
            HttpContext.Session.Remove(PendingRegistrationKey);
            return RedirectToAction("Index", "Home");
        }

        var existingEmail = await dbContext.Users
            .AnyAsync(x => x.Email != null && x.Email.ToLower() == pending.Email.ToLower());
        if (existingEmail)
        {
            TempData["Error"] = "Email is already registered.";
            HttpContext.Session.Remove(PendingRegistrationKey);
            return RedirectToAction("Index", "Home");
        }

        var user = new User
        {
            FullName = pending.FullName.Trim(),
            PhoneNumber = pending.PhoneNumber.Trim(),
            Email = pending.Email.Trim(),
            PasswordHash = pending.PasswordHash,
            Role = pending.Role,
            IsApproved = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        if (pending.Role == UserRole.Teacher)
        {
            dbContext.Teachers.Add(new Teacher
            {
                UserId = user.Id,
                TeacherId = await teacherCodeGenerator.GenerateNextAsync(),
                AssignedAtUtc = DateTime.UtcNow
            });
        }
        else if (pending.Role == UserRole.Student)
        {
            dbContext.Students.Add(new Student
            {
                UserId = user.Id,
                EnrollmentNumber = pending.EnrollmentNumber ?? string.Empty,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
        HttpContext.Session.Remove(PendingRegistrationKey);

        TempData["Success"] = "Registration completed. Wait for admin approval if required.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == model.PhoneNumber);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Phone number not found.");
            return View(model);
        }

        var otp = await otpService.GenerateOtpAsync(model.PhoneNumber);
        await otpService.SendOtpSmsPlaceholderAsync(model.PhoneNumber, otp);
        TempData["Success"] = "OTP generated and sent via SMS placeholder.";
        TempData["DebugOtp"] = otp;
        TempData["ResetPasswordPhone"] = model.PhoneNumber;

        return RedirectToAction(nameof(VerifyForgotPasswordOtp));
    }

    [HttpGet]
    public IActionResult VerifyForgotPasswordOtp()
    {
        var phone = TempData.Peek("ResetPasswordPhone")?.ToString();
        if (string.IsNullOrWhiteSpace(phone))
        {
            return RedirectToAction("Index", "Home");
        }
        
        return View(new VerifyForgotPasswordOtpViewModel { PhoneNumber = phone });
    }

    [HttpPost]
    public async Task<IActionResult> VerifyForgotPasswordOtp(VerifyForgotPasswordOtpViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var isValid = await otpService.VerifyOtpAsync(model.PhoneNumber, model.OtpCode);
        if (!isValid)
        {
            ModelState.AddModelError(string.Empty, "Invalid or expired OTP.");
            return View(model);
        }

        // Leave phone in TempData so ResetPassword can use it
        return RedirectToAction(nameof(ResetPassword));
    }

    [HttpGet]
    public IActionResult ResetPassword()
    {
        var phone = TempData.Peek("ResetPasswordPhone")?.ToString();
        if (string.IsNullOrWhiteSpace(phone))
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new ResetPasswordViewModel { PhoneNumber = phone });
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        
        var verifiedPhone = TempData.Peek("ResetPasswordPhone")?.ToString();
        if (verifiedPhone != model.PhoneNumber) 
        {
            return RedirectToAction("Index", "Home");
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == model.PhoneNumber);
        if (user is not null)
        {
            user.PasswordHash = PasswordService.HashPassword(model.NewPassword);
            await dbContext.SaveChangesAsync();
            
            TempData["Success"] = "Password reset successfully. Please login.";
            TempData.Remove("ResetPasswordPhone");
            return RedirectToAction("Index", "Home");
        }
        
        return View(model);
    }

    [HttpGet]
    public IActionResult ChangePassword(string? phoneNumber = null, bool firstLogin = false)
    {
        ViewBag.FirstLogin = firstLogin;
        return View(new ChangePasswordViewModel
        {
            PhoneNumber = phoneNumber ?? string.Empty
        });
    }

    [HttpGet("/Login")]
    public IActionResult Login() => View(new UnifiedLoginViewModel());

    [HttpPost("/Login")]
    public async Task<IActionResult> Login(UnifiedLoginViewModel model, [FromServices] IStudentSessionTracker sessionTracker)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await FindUserByIdentifierAsync(model.PhoneNumber);
        if (user is null || !PasswordService.VerifyPassword(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            return View(model);
        }

        if (user.Role == UserRole.Student)
        {
            var studentSessionId = GetOrCreateStudentSessionId();
            if (!sessionTracker.TryStartSession(user.Id, studentSessionId))
            {
                ModelState.AddModelError(string.Empty, "Student is already logged in on another device.");
                return View(model);
            }
        }

        SignInUser(user);

        if (user.IsFirstLogin && user.Role != UserRole.Admin)
        {
            TempData["Success"] = "Please change your initial password to complete first-time registration.";
            return RedirectToAction(nameof(ChangePassword), new { phoneNumber = user.PhoneNumber, firstLogin = true });
        }

        if (user.Role == UserRole.Admin)
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        if (user.Role == UserRole.Teacher)
        {
            return RedirectToAction("Dashboard", "Teacher");
        }

        return RedirectToAction("Dashboard", "Student");
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await FindUserByIdentifierAsync(model.PhoneNumber);
        if (user is null || !PasswordService.VerifyPassword(model.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid phone number or current password.");
            return View(model);
        }

        user.PasswordHash = PasswordService.HashPassword(model.NewPassword);
        user.IsFirstLogin = false;
        await dbContext.SaveChangesAsync();

        TempData["Success"] = "Password changed successfully.";
        
        if (user.Role == UserRole.Admin) return RedirectToAction("Dashboard", "Admin");
        if (user.Role == UserRole.Teacher) return RedirectToAction("Dashboard", "Teacher");
        if (user.Role == UserRole.Student) return RedirectToAction("Dashboard", "Student");
        
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("/Admin/Login")]
    public IActionResult AdminLogin() => RedirectToAction(nameof(Login));

    [HttpPost("/Admin/Login")]
    public async Task<IActionResult> AdminLogin(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await FindUserByIdentifierAsync(model.PhoneNumber, UserRole.Admin);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            return View(model);
        }

        if (!PasswordService.VerifyPassword(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            return View(model);
        }

        SignInUser(user);
        return RedirectToAction("Dashboard", "Admin");
    }

    [HttpGet("/Teacher/Login")]
    public IActionResult TeacherLogin() => RedirectToAction(nameof(Login));

    [HttpPost("/Teacher/Login")]
    public async Task<IActionResult> TeacherLogin(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await FindUserByIdentifierAsync(model.PhoneNumber, UserRole.Teacher);
        if (user is null || !PasswordService.VerifyPassword(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            return View(model);
        }

        SignInUser(user);

        if (user.IsFirstLogin)
        {
            TempData["Success"] = "Please change your initial password to complete first-time registration.";
            return RedirectToAction(nameof(ChangePassword), new { phoneNumber = user.PhoneNumber, firstLogin = true });
        }

        return RedirectToAction("Dashboard", "Teacher");
    }

    [HttpGet("/Student/Login")]
    public IActionResult StudentLogin() => RedirectToAction(nameof(Login));

    [HttpPost("/Student/Login")]
    public async Task<IActionResult> StudentLogin(StudentLoginViewModel model, [FromServices] IStudentSessionTracker sessionTracker)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await FindUserByIdentifierAsync(model.PhoneNumber, UserRole.Student);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            return View(model);
        }

        if (!PasswordService.VerifyPassword(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            return View(model);
        }

        var studentSessionId = GetOrCreateStudentSessionId();
        if (!sessionTracker.CanStartSession(user.Id, studentSessionId))
        {
            ModelState.AddModelError(string.Empty, "Student is already logged in on another device.");
            return View(model);
        }

        var otp = await otpService.GenerateOtpAsync(user.PhoneNumber);
        await otpService.SendOtpSmsPlaceholderAsync(user.PhoneNumber, otp);
        TempData["Success"] = "OTP generated and sent via SMS placeholder.";
        TempData["DebugOtp"] = otp;
        TempData[PendingStudentPhoneKey] = user.PhoneNumber;

        return RedirectToAction(nameof(StudentVerifyOtp));
    }

    [HttpGet("/Student/VerifyOtp")]
    public IActionResult StudentVerifyOtp()
    {
        var phoneNumber = TempData.Peek(PendingStudentPhoneKey)?.ToString();
        var model = new StudentVerifyOtpViewModel
        {
            PhoneNumber = phoneNumber ?? string.Empty
        };

        return View(model);
    }

    [HttpPost("/Student/VerifyOtp")]
    public async Task<IActionResult> StudentVerifyOtp(StudentVerifyOtpViewModel model, [FromServices] IStudentSessionTracker sessionTracker)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == model.PhoneNumber && x.Role == UserRole.Student);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Student account not found.");
            return View(model);
        }

        var studentSessionId = GetOrCreateStudentSessionId();
        if (!sessionTracker.CanStartSession(user.Id, studentSessionId))
        {
            ModelState.AddModelError(string.Empty, "Student is already logged in on another device.");
            return View(model);
        }

        var isValid = await otpService.VerifyOtpAsync(model.PhoneNumber, model.OtpCode);
        if (!isValid)
        {
            ModelState.AddModelError(string.Empty, "Invalid or expired OTP.");
            return View(model);
        }

        if (!sessionTracker.TryStartSession(user.Id, studentSessionId))
        {
            ModelState.AddModelError(string.Empty, "Student is already logged in on another device.");
            return View(model);
        }

        SignInUser(user);

        if (user.IsFirstLogin)
        {
            TempData["Success"] = "Please change your initial password to complete first-time registration.";
            return RedirectToAction(nameof(ChangePassword), new { phoneNumber = user.PhoneNumber, firstLogin = true });
        }

        return RedirectToAction("Dashboard", "Student");
    }

    public IActionResult PendingApproval() => View();

    public IActionResult AccessDenied() => View();

    public async Task<IActionResult> Logout()
    {
        var sessionGuard = HttpContext.RequestServices.GetRequiredService<ISessionGuardService>();
        await sessionGuard.LogoutAsync(HttpContext);
        return RedirectToAction("Index", "Home");
    }

    private void SignInUser(User user)
    {
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Role", user.Role.ToString());
        HttpContext.Session.SetString("Name", user.FullName);
        HttpContext.Session.SetString("IsApproved", user.IsApproved.ToString());
    }

    private string GetOrCreateStudentSessionId()
    {
        var sessionId = HttpContext.Session.GetString(StudentSessionKey);
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            return sessionId;
        }

        sessionId = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString(StudentSessionKey, sessionId);
        return sessionId;
    }

    private async Task<User?> FindUserByIdentifierAsync(string identifier, UserRole? role = null)
    {
        var normalized = identifier.Trim();
        var query = dbContext.Users
            .Include(x => x.Teacher)
            .Include(x => x.Student)
            .AsQueryable();

        if (role.HasValue)
        {
            query = query.Where(x => x.Role == role.Value);
        }

        return await query.FirstOrDefaultAsync(x =>
            x.PhoneNumber == normalized
            || x.EnrollmentNo == normalized
            || (x.Teacher != null && x.Teacher.TeacherId == normalized)
            || (x.Student != null && x.Student.EnrollmentNumber == normalized));
    }
}
