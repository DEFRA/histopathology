using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Histo.Web.Auth;

public static class AuthorizationPolicies
{
    public const string MaintenanceOnly = "MaintenanceOnly";
    public const string HistoOrMaintenance = "HistoOrMaintenance";

    /// <summary>Applied to every endpoint that has no explicit [AllowAnonymous].</summary>
    public static AuthorizationPolicy FallbackRequireSignedIn() =>
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

    // Single definition of each role rule, shared by the named policies and the page filters.
    public static bool IsMaintenance(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true
        && user.FindFirst(AppClaimTypes.GroupName)?.Value == MaintenanceOnlyPageFilter.MaintenanceGroup;

    public static bool IsHistoOrMaintenance(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true
        && user.FindFirst(AppClaimTypes.GroupName)?.Value is HistoOrMaintenancePageFilter.HistoGroup or HistoOrMaintenancePageFilter.MaintenanceGroup;

    public static AuthorizationBuilder AddHistoPolicies(this AuthorizationBuilder builder) => builder
        .AddPolicy(MaintenanceOnly, p => p.RequireAssertion(c => IsMaintenance(c.User)))
        .AddPolicy(HistoOrMaintenance, p => p.RequireAssertion(c => IsHistoOrMaintenance(c.User)));
}
