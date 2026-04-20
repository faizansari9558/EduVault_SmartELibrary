using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SmartELibrary.Models;
using SmartELibrary.Services;

namespace SmartELibrary.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RoleAuthorizeAttribute(params UserRole[] allowedRoles) : Attribute, IAsyncAuthorizationFilter
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

        var roleValue = context.HttpContext.Session.GetString("Role");
        if (!Enum.TryParse<UserRole>(roleValue, out var role) || !allowedRoles.Contains(role))
        {
            await sessionGuard.LogoutAsync(context.HttpContext);
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }
    }
}
