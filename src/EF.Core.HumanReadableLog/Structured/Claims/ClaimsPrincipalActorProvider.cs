using System.Security.Claims;

namespace EF.Core.HumanReadableLog.Structured;

/// <summary>
/// Actor provider that extracts an identifier from a ClaimsPrincipal.
/// The principal is supplied by a delegate (e.g., from IHttpContextAccessor in ASP.NET).
/// </summary>
public sealed class ClaimsPrincipalActorProvider(Func<ClaimsPrincipal?> principalAccessor) : IAuditActorProvider
{
    private readonly Func<ClaimsPrincipal?> _principalAccessor = principalAccessor ?? (() => null);

    public string? GetActor()
    {
        var user = _principalAccessor();
        if (user is null) return null;
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.Identity?.Name;
    }
}
