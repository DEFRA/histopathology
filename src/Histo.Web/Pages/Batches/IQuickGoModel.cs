namespace Histo.Web.Pages.Batches;

/// <summary>Quick-Go (direct navigation by submission number) members consumed by <c>_QuickGoForm.cshtml</c>.</summary>
public interface IQuickGoModel
{
    int? QuickGoId { get; set; }
    string? GoError { get; }
}
