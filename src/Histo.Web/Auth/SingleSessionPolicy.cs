using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;

namespace Histo.Web.Auth;

/// <summary>
/// One active sign-in per user: each sign-in issues a new stamp, and a cookie carrying an older
/// stamp is rejected. A missing stamp (cache restart/expiry) is treated as valid so users are
/// never locked out by a cache miss.
/// </summary>
public static class SingleSessionPolicy
{
    private static readonly DistributedCacheEntryOptions StampLifetime = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
    };

    private static string Key(string userId) => $"session-stamp:{userId}";

    public static async Task<string> IssueStampAsync(IDistributedCache cache, string userId)
    {
        var stamp = Guid.NewGuid().ToString("N");
        await cache.SetStringAsync(Key(userId), stamp, StampLifetime);
        return stamp;
    }

    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var userId = principal?.FindFirst(AppClaimTypes.UserDbId)?.Value;
        var stamp = principal?.FindFirst(AppClaimTypes.SessionStamp)?.Value;
        if (userId is null || stamp is null) return;

        var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
        var current = await cache.GetStringAsync(Key(userId));
        if (current is not null && current != stamp)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(context.Scheme.Name);
        }
    }

    public static Claim StampClaim(string stamp) => new(AppClaimTypes.SessionStamp, stamp);
}
