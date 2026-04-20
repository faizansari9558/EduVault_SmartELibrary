using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SmartELibrary.Services;

namespace SmartELibrary.Filters;

public class StudentSemesterApprovalFilter(IStudentSemesterApprovalService approvalService) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var sessionGuard = context.HttpContext.RequestServices.GetRequiredService<ISessionGuardService>();
        var isValid = await sessionGuard.HasValidAuthenticatedSessionAsync(context.HttpContext);

        if (!isValid)
        {
            await sessionGuard.LogoutAsync(context.HttpContext);
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }

        var session = context.HttpContext.Session;
        var userId = session.GetInt32("UserId");
        var role = session.GetString("Role");

        if (!string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (userId is null)
        {
            await sessionGuard.LogoutAsync(context.HttpContext);
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }

        var hasApproved = await approvalService.HasApprovedEnrollmentAsync(userId.Value);
        if (!hasApproved)
        {
            await sessionGuard.LogoutAsync(context.HttpContext);
            context.Result = new RedirectToActionResult("Login", "Auth", null);
        }
    }
}
