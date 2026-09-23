namespace Histo.Core.Domain;

/// <summary>
/// Pure domain helper enforcing the Group→Area whitelist introduced when the
/// "Mouse Bioassay" and "Neuropath" user areas were removed.
///
/// Per business decision (2026-09-18): any known Group may cross-map to any of the 4
/// active areas (External Customer, TB Diagnostics, Histopath, Other VLA) — legacy itself
/// had no cross-field validation between Group and Area at all, so a strict 1:1 whitelist
/// (e.g. Customer → External Customer/TB Diagnostics only) over-restricts real combinations
/// that existed in legacy (e.g. Customer → Histopath, Maintenance → Histopath). This helper
/// now only guards against an unrecognised Group name or a retired/unknown Area name.
///
/// See docs/Mouse-Bioassay-Neuropath-Removal-Analysis.md, section 3, item 3.
/// </summary>
public static class GroupAreaMappingHelpers
{
    private static readonly string[] KnownGroups =
        ["Customer", "Histopathology User", "Maintenance"];

    private static readonly string[] AllowedAreas =
        ["External Customer", "TB Diagnostics", "Histopath", "Other VLA"];

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="groupName"/> is a recognised
    /// group and <paramref name="areaName"/> is one of the 4 active areas — any group may be
    /// paired with any active area. Fails closed (returns <see langword="false"/>) for
    /// null/blank input or an unrecognised group/area name.
    /// </summary>
    public static bool IsAllowedCombination(string? groupName, string? areaName)
    {
        if (string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(areaName))
            return false;

        return KnownGroups.Contains(groupName, StringComparer.OrdinalIgnoreCase)
            && AllowedAreas.Contains(areaName, StringComparer.OrdinalIgnoreCase);
    }
}
