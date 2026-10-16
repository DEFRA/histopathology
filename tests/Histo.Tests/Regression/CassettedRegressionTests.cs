using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Tier 2 gap closure: regression suite for Submission type (Cassetted) — step 1 of the
/// Create Submission journey, which previously only had atomic Unit/ coverage.
/// <see cref="ISessionService"/> uses <c>SetupAllProperties</c> so the real stateful
/// get/set round-trip the page relies on (BatchType, CassettedBatchTypeDraft, BatchID,
/// ReturnPage) is genuinely exercised rather than Moq's default no-op setters.
/// </summary>
public class CassettedRegressionTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();

    public CassettedRegressionTests()
    {
        _session.SetupAllProperties();
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[
                new LookupItem { Code = "SENT", Name = "Sent in" },
                new LookupItem { Code = "COLL", Name = "Collected" },
            ]);
    }

    private CassettedModel CreateSut()
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        return new(_session.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
    }

    [Fact]
    public async Task OnGetAsync_FreshVisit_DefaultsToTseAndClearsDraft()
    {
        _session.Object.CassettedBatchTypeDraft = 1; // stale leftover from an earlier journey
        var sut = CreateSut();

        await sut.OnGetAsync(restore: false);

        Assert.Equal(BatchTypeConstants.Tse, sut.BatchType);
        Assert.Null(_session.Object.CassettedBatchTypeDraft);
    }

    [Fact]
    public async Task OnGetAsync_RestoreTrue_RestoresDraftBatchTypeAndSubmittedAsCode()
    {
        _session.Object.CassettedBatchTypeDraft = BatchTypeConstants.NonTse;
        var sut = CreateSut();
        sut.TempData["CreateSubmittedAsCode"] = "SENT";

        await sut.OnGetAsync(restore: true);

        Assert.Equal(BatchTypeConstants.NonTse, sut.BatchType);
        Assert.Equal("SENT", sut.SubmittedAs);
    }

    [Fact]
    public async Task OnPostAsync_NoSubmittedAsSelected_IsRejected()
    {
        var sut = CreateSut();
        sut.SubmittedAs = null;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select a submission type.", sut.Errors["SubmittedAs"]);
    }

    [Fact]
    public async Task OnPostAsync_UnknownSubmittedAsCode_IsRejected()
    {
        var sut = CreateSut();
        sut.SubmittedAs = "NOT-A-REAL-CODE";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(sut.Errors.ContainsKey("SubmittedAs"));
    }

    [Fact]
    public async Task OnPostAsync_ValidSelection_StoresSessionStateAndRedirectsToBatchDetailsCreateMode()
    {
        var sut = CreateSut();
        sut.BatchType = BatchTypeConstants.NonTse;
        sut.SubmittedAs = "sent"; // case/whitespace-insensitive match against the lookup Code
        _session.Object.BatchID = 999; // stale batch from a previous journey — must be cleared
        _session.Object.ReturnPage = "/Batches/ViewSubmissions"; // stale back-link context — must be cleared

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchDetails", redirect.PageName);
        Assert.Equal("create", redirect.RouteValues?["mode"]);
        Assert.Equal(BatchTypeConstants.NonTse, _session.Object.BatchType);
        Assert.Equal(BatchTypeConstants.NonTse, _session.Object.CassettedBatchTypeDraft);
        Assert.Null(_session.Object.BatchID);
        Assert.Equal(string.Empty, _session.Object.ReturnPage);
        Assert.Equal("SENT", sut.TempData["CreateSubmittedAsCode"]);
    }
}
