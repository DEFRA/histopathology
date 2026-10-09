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
    private readonly IBlockService _blocks;

    public BookBlockRefModel(ISessionService session, IBlockService blocks)
        : base(session) { _blocks = blocks; }

    [BindProperty] public string SenderRefFrom { get; set; } = string.Empty;
    [BindProperty] public string? SenderRefTo { get; set; }
    [BindProperty] public string BlockRefFrom { get; set; } = string.Empty;
    [BindProperty] public string? BlockRefTo { get; set; }

    public string? Error { get; private set; }
    public string? SuccessMessage { get; private set; }
    public List<string> ResultMessages { get; } = [];

    public void OnGet()
    {
        ViewData["Title"] = "Book blocks";
        ViewData["PageTitle"] = "Book blocks";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Book blocks";
        ViewData["PageTitle"] = "Book blocks";

        if (string.IsNullOrWhiteSpace(SenderRefFrom))
        {
            Error = "Enter a Sender Ref from.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(BlockRefFrom) || !SenderRefHelpers.IsValidBlockRef(BlockRefFrom.Trim()))
        {
            Error = "The requested block ref range cannot be created.";
            return Page();
        }
        var blockRefFrom = int.Parse(BlockRefFrom.Trim());

        int blockRefTo;
        if (!string.IsNullOrWhiteSpace(BlockRefTo))
        {
            if (!SenderRefHelpers.IsValidBlockRef(BlockRefTo.Trim()))
            {
                Error = "The requested block ref range cannot be created.";
                return Page();
            }
            blockRefTo = int.Parse(BlockRefTo.Trim());
            if (blockRefTo <= blockRefFrom)
            {
                Error = "The requested block ref range cannot be created.";
                return Page();
            }
        }
        else
        {
            blockRefTo = blockRefFrom;
        }

        // Resolve the Sender Ref range to process — mirrors ProcessMultipleBookings/
        // ValidatePGNumberRange/ValidateMouseNumberRange (legacy source above).
        var senderRefFrom = SenderRefFrom.Trim();
        var senderRefTo = SenderRefTo?.Trim();
        List<string>? senderRefs;

        if (SenderRefHelpers.IsPgNumber(senderRefFrom))
        {
            if (!SenderRefHelpers.TryParsePgNumber(senderRefFrom, out var idFrom, out var year))
            {
                Error = "The range requested cannot be created.";
                return Page();
            }

            var idTo = idFrom;
            if (!string.IsNullOrEmpty(senderRefTo))
            {
                if (!SenderRefHelpers.IsPgNumber(senderRefTo) || !SenderRefHelpers.TryParsePgNumber(senderRefTo, out idTo, out var yearTo))
                {
                    Error = "The range requested cannot be created.";
                    return Page();
                }
                if (yearTo != year) { Error = "PG Number years must be the same."; return Page(); }
                if (idTo <= idFrom) { Error = "The requested sender ref range cannot be created."; return Page(); }
            }

            senderRefs = [.. Enumerable.Range(idFrom, idTo - idFrom + 1).Select(i => SenderRefHelpers.FormatPgNumber(i, year))];
        }
        else if (SenderRefHelpers.IsMouseNumber(senderRefFrom))
        {
            if (!SenderRefHelpers.TryParseMouseNumber(senderRefFrom, out var idFrom))
            {
                Error = "The range requested cannot be created.";
                return Page();
            }

            var idTo = idFrom;
            if (!string.IsNullOrEmpty(senderRefTo))
            {
                if (!SenderRefHelpers.IsMouseNumber(senderRefTo) || !SenderRefHelpers.TryParseMouseNumber(senderRefTo, out idTo))
                {
                    Error = "The range requested cannot be created.";
                    return Page();
                }
                if (idTo <= idFrom) { Error = "The requested sender ref range cannot be created."; return Page(); }
            }

            senderRefs = [.. Enumerable.Range(idFrom, idTo - idFrom + 1).Select(SenderRefHelpers.FormatMouseNumber)];
        }
        else
        {
            // Legacy note (BookBlockRef.aspx): "If an alternative range is used, only the first
            // Sender Ref in the range will have blocks booked" — a non-PG/non-Mouse Sender Ref To
            // is silently ignored; only SenderRefFrom itself is processed.
            senderRefs = [senderRefFrom];
        }

        var requestedRefs = Enumerable.Range(blockRefFrom, blockRefTo - blockRefFrom + 1).ToList();
        foreach (var senderRef in senderRefs)
        {
            // One atomic call: the duplicate check, sample resolution and inserts all run on a
            // single locked transaction, so concurrent bookings cannot both claim the same ref.
            var results = await _blocks.BookPreBookedBlocksAsync(senderRef, requestedRefs);
            if (results is null)
            {
                ResultMessages.Add($"Sample: {senderRef} — booking failed, so no blocks were booked.");
                continue;
            }

            var numberSuccess = 0;
            var numberFails = 0;

            foreach (var result in results)
            {
                var blockRef = SenderRefHelpers.FormatBlockRef(result.BlockRef);
                switch (result.Outcome)
                {
                    case PreBookedBlockOutcome.Booked:
                        numberSuccess++;
                        break;
                    case PreBookedBlockOutcome.AlreadyExists:
                        ResultMessages.Add($"Sample: {senderRef} Block {blockRef} not booked as it already exists.");
                        numberFails++;
                        break;
                    default:
                        ResultMessages.Add($"Sample: {senderRef} — failed to retrieve or create sample data.");
                        numberFails++;
                        break;
                }
            }

            ResultMessages.Add($"Sample: {senderRef} {numberSuccess} blocks booked, {numberFails} blocks not booked.");
        }

        var requested = (senderRefs?.Count ?? 0) * (blockRefTo - blockRefFrom + 1);
        var anyFailed = ResultMessages.Exists(m =>
            m.Contains("not booked", StringComparison.OrdinalIgnoreCase)
            || m.Contains("failed", StringComparison.OrdinalIgnoreCase));
        SuccessMessage = requested == 0 ? "No blocks were requested." : (anyFailed ? null : "Blocks booked successfully.");
        return Page();
    }
}
