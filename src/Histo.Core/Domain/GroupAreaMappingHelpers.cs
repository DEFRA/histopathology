namespace Histo.Core.Domain;

/// <summary>
/// Pure domain helper enforcing the Group→Area whitelist introduced when the
/// "Mouse Bioassay" and "Neuropath" user areas were removed, restricting the
/// system to 4 areas (External Customer, Histopath, Other VLA, TB Diagnostics).
///
/// Prior to this change, Group and User Area were two independent DB-driven
/// lookups with no cross-field validation anywhere in the codebase — this is
/// net-new business logic, not a relaxation of an existing rule.
///
/// See docs/Mouse-Bioassay-Neuropath-Removal-Analysis.md, section 3, item 3.
/// </summary>
public static class GroupAreaMappingHelpers
{
    private static readonly Dictionary<string, string[]> AllowedAreasByGroup =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Customer"] = ["External Customer", "TB Diagnostics"],
            ["Histopathology User"] = ["Histopath"],
            ["Maintenance"] = ["Other VLA"],
        };

    /// <summary>
    /// Returns <see langword="true"/> only when <paramref name="areaName"/> is one of the
    /// areas permitted for <paramref name="groupName"/>. Fails closed (returns
    /// <see langword="false"/>) for null/blank input or an unrecognised group name.
    /// </summary>
    public static bool IsAllowedCombination(string? groupName, string? areaName)
    {
        if (string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(areaName))
            return false;

        return AllowedAreasByGroup.TryGetValue(groupName, out var allowedAreas)
            && allowedAreas.Contains(areaName, StringComparer.OrdinalIgnoreCase);
    }
}
