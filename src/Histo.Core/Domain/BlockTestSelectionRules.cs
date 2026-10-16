namespace Histo.Core.Domain;

/// <summary>Tests to insert on one block — never deletions.</summary>
public sealed record BlockTestAdditions(IReadOnlyList<string> Histology, IReadOnlyList<string> Antibodies, IReadOnlyList<string> Stains)
{
    public bool IsEmpty => Histology.Count == 0 && Antibodies.Count == 0 && Stains.Count == 0;
}

/// <summary>
/// Block test rules applied when tests newly added to a submission are propagated to its existing blocks.
/// Codes mirror <c>luHistology</c>: 1 EO, 3 Special Stain, 4 IHC-PrP, 6 IHC-Other, 7 Archive.
/// </summary>
public static class BlockTestSelectionRules
{
    private const string Eo = "1";
    private const string SpecialStain = "3";
    private const string IhcPrp = "4";
    private const string IhcOther = "6";
    private const string Archive = "7";

    private static bool IsExclusive(string code) => code == Eo || code == Archive;

    /// <summary>
    /// What to add to a block that already has the given tests. EO and Archive can't be combined with other
    /// histology, antibodies need IHC and stains need Special Stain (legacy BlockDetails rules).
    /// </summary>
    public static BlockTestAdditions ComputeAdditions(
        IEnumerable<string> currentHistology, IEnumerable<string> currentAntibodies, IEnumerable<string> currentStains,
        IEnumerable<string> addedHistology, IEnumerable<string> addedAntibodies, IEnumerable<string> addedStains)
    {
        var cmp = StringComparer.OrdinalIgnoreCase;
        var histology = currentHistology.ToHashSet(cmp);
        var antibodies = currentAntibodies.ToHashSet(cmp);
        var stains = currentStains.ToHashSet(cmp);

        var newHistology = new List<string>();
        if (!histology.Any(IsExclusive))
        {
            newHistology.AddRange(addedHistology.Where(c => !IsExclusive(c) && !histology.Contains(c)).Distinct(cmp));
            if (histology.Count == 0 && newHistology.Count == 0)
                newHistology.AddRange(addedHistology.Where(IsExclusive).Take(1));
        }

        var finalHistology = histology.Concat(newHistology).ToHashSet(cmp);
        var newAntibodies = finalHistology.Contains(IhcPrp) || finalHistology.Contains(IhcOther)
            ? addedAntibodies.Where(c => !antibodies.Contains(c)).Distinct(cmp).ToList()
            : [];
        var newStains = finalHistology.Contains(SpecialStain)
            ? addedStains.Where(c => !stains.Contains(c)).Distinct(cmp).ToList()
            : [];

        return new BlockTestAdditions(newHistology, newAntibodies, newStains);
    }
}
