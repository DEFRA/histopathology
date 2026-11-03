using Histo.Submissions.Models;

namespace Histo.Web.Pages.Shared;

/// <summary>Model for <c>_BlockTissuesCell.cshtml</c>.</summary>
public sealed record BlockTissuesCell(IReadOnlyList<Tissue> Tissues, Func<string, string> TissueName);

/// <summary>Model for <c>_BlockHistologyCells.cshtml</c>.</summary>
public sealed record BlockHistologyCells(int BlockId, IReadOnlyList<string> Codes, bool IsTse);
