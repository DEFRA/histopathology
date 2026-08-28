namespace Histo.Core.Domain;

/// <summary>
/// Pure object-level access decision for batch-scoped pages that accept a batch ID from the
/// URL (route/query) rather than only from session state. Extracted as a pure function so it
/// can be unit tested without a Razor Pages <c>HttpContext</c>/<c>PageModel</c> harness.
/// See <c>Histo.Web.Pages.HistoPageModel.CheckBatchAccessAsync</c> for the calling wrapper.
/// </summary>
public static class BatchAccessDecision
{
    /// <summary>
    /// Returns <see langword="true"/> when the caller may access a batch belonging to
    /// <paramref name="batchUserAreaCode"/>. Histo users (lab staff) see every area;
    /// all other roles are restricted to their own <paramref name="callerUserAreaId"/>.
    /// A <see langword="null"/> <paramref name="batchUserAreaCode"/> (batch not found) is
    /// allowed through — the caller's own not-found handling applies instead.
    /// </summary>
    public static bool IsAllowed(bool isHistoUser, int? batchUserAreaCode, int callerUserAreaId)
    {
        if (isHistoUser) return true;
        if (batchUserAreaCode is null) return true;
        return batchUserAreaCode.Value == callerUserAreaId;
    }
}
