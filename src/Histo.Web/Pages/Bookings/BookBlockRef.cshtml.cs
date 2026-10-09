using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Submissions.Interfaces;
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
    private readonly ISubmissionService _submissions;

    public BookBlockRefModel(ISessionService session, IBlockService blocks, ISubmissionService submissions)
        : base(session) { _blocks = blocks; _submissions = submissions; }

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

        var userId = Session.UserID;
        foreach (var senderRef in senderRefs)
        {
            // A ref is taken whatever its status and whichever Animal row owns it: blocks claimed by a
            // real submission are Status Used against a BatchID, so the pre-booked placeholder lookup
            // (GetAnimalPreBookedBlocks, Status 2/3 only) never sees them.
            var existingRefs = await _blocks.TryGetUsedBlockRefsBySenderRefAsync(senderRef);
            if (existingRefs is null)
            {
                ResultMessages.Add($"Sample: {senderRef} — existing block ref check failed, so no blocks were booked.");
                continue;
            }

            var takenRefs = existingRefs.Select(b => b.BlockRef).ToHashSet();
            var numberFails = 0;
            var numberSuccess = 0;
            var animalId = 0;

            for (var blockRefNum = blockRefFrom; blockRefNum <= blockRefTo; blockRefNum++)
            {
                var blockRef = SenderRefHelpers.FormatBlockRef(blockRefNum);

                if (takenRefs.Contains(blockRefNum))
                {
                    ResultMessages.Add($"Sample: {senderRef} Block {blockRef} not booked as it already exists.");
                    numberFails++;
                    continue;
                }

                // Deferred so a fully-duplicate range leaves no orphan Animal row behind.
                if (animalId == 0) animalId = await ResolveAnimalIdAsync(senderRef, userId);
                if (animalId == 0)
                {
                    ResultMessages.Add($"Sample: {senderRef} — failed to retrieve or create sample data.");
                    numberFails++;
                    continue;
                }

                if (await _blocks.CreatePreBookedBlockAsync(animalId, blockRef))
                {
                    numberSuccess++;
                }
                else
                {
                    ResultMessages.Add($"Sample: {senderRef} Block {blockRef} not booked.");
                    numberFails++;
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

    /// <summary>
    /// Picks the Animal row new placeholders hang off. Legacy source: ProcessMultipleBookings —
    /// <c>dtAnimaldata.Rows(0)("ID")</c>, i.e. the first GetAnimalBySender match, creating the row
    /// only when the sender ref is unknown.
    /// </summary>
    private async Task<int> ResolveAnimalIdAsync(string senderRef, int userId)
    {
        var existing = await _submissions.GetAnimalBySenderAsync(senderRef);
        return existing.FirstOrDefault()?.ID
            ?? await _submissions.AddAnimalAsync(batchSubmissionId: 0, senderRef, userId);
    }
}
