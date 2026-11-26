using System.Globalization;
using System.Security.Claims;
using ReturnFlow.Api.Domain;

namespace ReturnFlow.Api.Services;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue("sub")
                  ?? throw new InvalidOperationException("Missing 'sub' claim."), CultureInfo.InvariantCulture);

    public static bool IsStaff(this ClaimsPrincipal principal) =>
        principal.IsInRole(nameof(Role.Agent))
        || principal.IsInRole(nameof(Role.Manager))
        || principal.IsInRole(nameof(Role.Admin));
}
