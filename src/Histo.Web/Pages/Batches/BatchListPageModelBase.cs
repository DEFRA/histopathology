using System.Diagnostics.CodeAnalysis;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>Batch grid with a Quick-Go box — state and plumbing shared by the Batches* list pages.</summary>
public abstract class BatchListPageModelBase : GridPageModel, IQuickGoModel
{
    protected readonly IBatchService BatchService;

    protected BatchListPageModelBase(ISessionService session, IBatchService batches)
        : base(session) => BatchService = batches;

    public IReadOnlyList<BatchListResult> Batches { get; private set; } = [];

    public int TotalCount => Batches.Count;

    /// <summary>Quick-Go: direct navigation by submission number.</summary>
    [BindProperty]
    public int? QuickGoId { get; set; }

    /// <summary>Inline error message for Quick-Go validation failures.</summary>
    public string? GoError { get; protected set; }

    protected abstract string ListTitle { get; }

    protected abstract Task<IReadOnlyList<BatchListResult>> FetchBatchesAsync();

    protected async Task LoadListAsync()
    {
        ViewData["Title"] = ListTitle;
        ViewData["PageTitle"] = ListTitle;
        Batches = await FetchBatchesAsync();
        PopulateGridViewData(TotalCount);
    }

    /// <summary>Sets the Quick-Go error and returns <c>true</c> when no positive submission number was entered.</summary>
    [MemberNotNullWhen(false, nameof(QuickGoId))]
    protected bool IsQuickGoIdMissing()
    {
        if (QuickGoId.HasValue && QuickGoId.Value > 0) return false;
        GoError = "Enter a submission number.";
        return true;
    }

    protected IOrderedEnumerable<BatchListResult> OrderBatches<TKey>(Func<BatchListResult, TKey> key) =>
        SortDesc ? Batches.OrderByDescending(key) : Batches.OrderBy(key);

    protected static IReadOnlyList<BatchListResult> TakePage(IEnumerable<BatchListResult> ordered, int pageNumber) =>
        ordered.Skip((pageNumber - 1) * PageSize).Take(PageSize).ToList();
}
