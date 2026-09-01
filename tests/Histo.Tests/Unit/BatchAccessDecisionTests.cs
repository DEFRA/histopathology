using Histo.Core.Domain;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="BatchAccessDecision"/> — the object-level access guard used by
/// <c>Histo.Web.Pages.HistoPageModel.CheckBatchAccessAsync</c> for batch-scoped pages that accept
/// a batch ID from the URL (route/query) rather than only from session state.
/// </summary>
public class BatchAccessDecisionTests
{
    [Fact]
    public void IsAllowed_HistoUser_AlwaysAllowed()
    {
        var result = BatchAccessDecision.IsAllowed(isHistoUser: true, batchUserAreaCode: 99, callerUserAreaId: 1);
        Assert.True(result);
    }

    [Fact]
    public void IsAllowed_NonHistoUser_MatchingArea_Allowed()
    {
        var result = BatchAccessDecision.IsAllowed(isHistoUser: false, batchUserAreaCode: 5, callerUserAreaId: 5);
        Assert.True(result);
    }

    [Fact]
    public void IsAllowed_NonHistoUser_MismatchedArea_Denied()
    {
        var result = BatchAccessDecision.IsAllowed(isHistoUser: false, batchUserAreaCode: 5, callerUserAreaId: 6);
        Assert.False(result);
    }

    [Fact]
    public void IsAllowed_BatchNotFound_AllowedThroughToPagesOwnNotFoundHandling()
    {
        var result = BatchAccessDecision.IsAllowed(isHistoUser: false, batchUserAreaCode: null, callerUserAreaId: 6);
        Assert.True(result);
    }
}
