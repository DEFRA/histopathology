using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Bookings;

/// <summary>
/// Replaces <c>BookBlockRef.aspx</c> — a standalone admin utility that pre-books new placeholder
/// blocks for a range of Sender Refs (plain, PG-number, or Mouse-number format), creating the
/// Animal record first if it doesn't already exist. Not linked to any specific batch — the
/// blocks created here have no <c>BatchID</c> until a real submission later claims the sender ref.
///
/// Legacy source: BookBlockRef.aspx.vb::ProcessMultipleBookings/btnOk_Click.
/// </summary>
public class BookBlockRefModel : HistoPageModel
{
    private const string BookBlocksTitle = "Book blocks";
    private const string RangeCannotBeCreatedMessage = "The range requested cannot be created.";

    private readonly IBlockService _blocks;

    public BookBlockRefModel(ISessionService session, IBlockService blocks)
        : base(session) { _blocks = blocks; }

    [BindProperty(SupportsGet = true)] public string SenderRefFrom { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public string? SenderRefTo { get; set; }
    [BindProperty(SupportsGet = true)] public string BlockRefFrom { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public string? BlockRefTo { get; set; }

    public string? Error { get; private set; }
    public string? SuccessMessage { get; private set; }
    public List<string> ResultMessages { get; } = [];

    public void OnGet()
    {
        ViewData["Title"] = BookBlocksTitle;
        ViewData["PageTitle"] = BookBlocksTitle;
    }

    // Legacy source: BookBlockRef.aspx.vb::lbSearchBlockRefs_Click — a full postback that
    // carries whatever is currently typed in SenderRefFrom, even if "Book" was never clicked.
    // ReturnPage carries all 4 fields back so Back from SearchBlockRefs restores this form
    // instead of landing on it empty (fields are SupportsGet so they rebind from this query string).
    public IActionResult OnPostCheckUsedBlockRefs()
    {
        // SupportsGet can bind these non-nullable strings to null when the request omits the key
        // (ASP.NET Core's implicit-required-for-non-nullable-reference-types behaviour).
        var returnPage = $"/Bookings/BookBlockRef?SenderRefFrom={Uri.EscapeDataString(SenderRefFrom ?? string.Empty)}" +
            $"&SenderRefTo={Uri.EscapeDataString(SenderRefTo ?? string.Empty)}" +
            $"&BlockRefFrom={Uri.EscapeDataString(BlockRefFrom ?? string.Empty)}" +
            $"&BlockRefTo={Uri.EscapeDataString(BlockRefTo ?? string.Empty)}";
        return RedirectToPage("/Search/SearchBlockRefs", new { SenderRef = SenderRefFrom, ReturnPage = returnPage });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = BookBlocksTitle;
        ViewData["PageTitle"] = BookBlocksTitle;

        if (string.IsNullOrWhiteSpace(SenderRefFrom))
        {
            Error = "Enter a Sender Ref from.";
            return Page();
        }

        var blockRangeError = ResolveBlockRefRange(BlockRefFrom, BlockRefTo, out var blockRefFrom, out var blockRefTo);
        if (blockRangeError is not null)
        {
            Error = blockRangeError;
            return Page();
        }

        // Resolve the Sender Ref range to process — mirrors ProcessMultipleBookings/
        // ValidatePGNumberRange/ValidateMouseNumberRange (legacy source above).
        var senderRangeError = ResolveSenderRefs(SenderRefFrom.Trim(), SenderRefTo?.Trim(), out var senderRefs);
        if (senderRangeError is not null)
        {
            Error = senderRangeError;
            return Page();
        }

        var requestedRefs = Enumerable.Range(blockRefFrom, blockRefTo - blockRefFrom + 1).ToList();
        foreach (var senderRef in senderRefs)
            await BookSenderRefAsync(senderRef, requestedRefs);

        SuccessMessage = BuildSuccessMessage(senderRefs.Count * (blockRefTo - blockRefFrom + 1));
        return Page();
    }

    private const string InvalidBlockRangeMessage = "The requested block ref range cannot be created.";
    private const string InvalidSenderRangeMessage = "The requested sender ref range cannot be created.";

    private static string? ResolveBlockRefRange(string? from, string? to, out int blockRefFrom, out int blockRefTo)
    {
        blockRefFrom = 0;
        blockRefTo = 0;
        if (string.IsNullOrWhiteSpace(from) || !SenderRefHelpers.IsValidBlockRef(from.Trim()))
            return InvalidBlockRangeMessage;
        blockRefFrom = int.Parse(from.Trim());

        if (string.IsNullOrWhiteSpace(to))
        {
            blockRefTo = blockRefFrom;
            return null;
        }

        if (!SenderRefHelpers.IsValidBlockRef(to.Trim())) return InvalidBlockRangeMessage;
        blockRefTo = int.Parse(to.Trim());
        return blockRefTo <= blockRefFrom ? InvalidBlockRangeMessage : null;
    }

    private static string? ResolveSenderRefs(string from, string? to, out List<string> senderRefs)
    {
        senderRefs = [];
        if (SenderRefHelpers.IsPgNumber(from)) return ResolvePgRange(from, to, senderRefs);
        if (SenderRefHelpers.IsMouseNumber(from)) return ResolveMouseRange(from, to, senderRefs);

        // Legacy note (BookBlockRef.aspx): "If an alternative range is used, only the first
        // Sender Ref in the range will have blocks booked" — a non-PG/non-Mouse Sender Ref To
        // is silently ignored; only SenderRefFrom itself is processed.
        senderRefs.Add(from);
        return null;
    }

    private static string? ResolvePgRange(string from, string? to, List<string> senderRefs)
    {
        if (!SenderRefHelpers.TryParsePgNumber(from, out var idFrom, out var year))
            return RangeCannotBeCreatedMessage;

        var idTo = idFrom;
        if (!string.IsNullOrEmpty(to))
        {
            if (!SenderRefHelpers.IsPgNumber(to) || !SenderRefHelpers.TryParsePgNumber(to, out idTo, out var yearTo))
                return RangeCannotBeCreatedMessage;
            if (yearTo != year) return "PG Number years must be the same.";
            if (idTo <= idFrom) return InvalidSenderRangeMessage;
        }

        senderRefs.AddRange(Enumerable.Range(idFrom, idTo - idFrom + 1).Select(i => SenderRefHelpers.FormatPgNumber(i, year)));
        return null;
    }

    private static string? ResolveMouseRange(string from, string? to, List<string> senderRefs)
    {
        if (!SenderRefHelpers.TryParseMouseNumber(from, out var idFrom))
            return RangeCannotBeCreatedMessage;

        var idTo = idFrom;
        if (!string.IsNullOrEmpty(to))
        {
            if (!SenderRefHelpers.IsMouseNumber(to) || !SenderRefHelpers.TryParseMouseNumber(to, out idTo))
                return RangeCannotBeCreatedMessage;
            if (idTo <= idFrom) return InvalidSenderRangeMessage;
        }

        senderRefs.AddRange(Enumerable.Range(idFrom, idTo - idFrom + 1).Select(SenderRefHelpers.FormatMouseNumber));
        return null;
    }

    private async Task BookSenderRefAsync(string senderRef, List<int> requestedRefs)
    {
        // One atomic call: the duplicate check, sample resolution and inserts all run on a
        // single locked transaction, so concurrent bookings cannot both claim the same ref.
        var results = await _blocks.BookPreBookedBlocksAsync(senderRef, requestedRefs);
        if (results is null)
        {
            ResultMessages.Add($"Sample: {senderRef} — booking failed, so no blocks were booked.");
            return;
        }

        var numberSuccess = 0;
        var numberFails = 0;

        foreach (var result in results)
        {
            if (result.Outcome == PreBookedBlockOutcome.Booked)
            {
                numberSuccess++;
                continue;
            }

            ResultMessages.Add(result.Outcome == PreBookedBlockOutcome.AlreadyExists
                ? $"Sample: {senderRef} Block {SenderRefHelpers.FormatBlockRef(result.BlockRef)} not booked as it already exists."
                : $"Sample: {senderRef} — failed to retrieve or create sample data.");
            numberFails++;
        }

        ResultMessages.Add($"Sample: {senderRef} {numberSuccess} blocks booked, {numberFails} blocks not booked.");
    }

    private string? BuildSuccessMessage(int requested)
    {
        if (requested == 0) return "No blocks were requested.";

        var anyFailed = ResultMessages.Exists(m =>
            m.Contains("not booked", StringComparison.OrdinalIgnoreCase)
            || m.Contains("failed", StringComparison.OrdinalIgnoreCase));
        return anyFailed ? null : "Blocks booked successfully.";
    }
}
