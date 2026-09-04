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

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="CassettedModel"/> — step 1 of the New Submission journey.</summary>
public class CassettedModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();

    public CassettedModelTests()
    {
        _session.SetupProperty(s => s.BatchType);
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.ReturnPage, string.Empty);
    }

    private CassettedModel CreateSut() =>
        new(_session.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = Mock.Of<ITempDataDictionary>(),
        };

    private static LookupItem MakeOption(int id, string code) => new() { ID = id, Name = $"Option {id}", Code = code };

    [Fact]
    public async Task OnGetAsync_RestoresBatchTypeFromSession()
    {
        _session.Object.BatchType = BatchTypeConstants.NonTse;
        _lookups.Setup(l => l.GetLookupDataAsync(11, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(BatchTypeConstants.NonTse, sut.BatchType);
    }

    [Fact]
    public async Task OnPostAsync_NoSubmittedAsSelected_ReturnsPageWithError()
    {
        _lookups.Setup(l => l.GetLookupDataAsync(11, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[MakeOption(5, "5")]);
        var sut = CreateSut();
        sut.SubmittedAs = null;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(sut.Errors.ContainsKey("SubmittedAs"));
    }

    [Fact]
    public async Task OnPostAsync_SubmittedAsNotInOptions_ReturnsPageWithError()
    {
        _lookups.Setup(l => l.GetLookupDataAsync(11, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[MakeOption(5, "5")]);
        var sut = CreateSut();
        sut.SubmittedAs = 999;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(sut.Errors.ContainsKey("SubmittedAs"));
    }

    [Fact]
    public async Task OnPostAsync_ValidSelection_StoresSessionStateAndRedirects()
    {
        _lookups.Setup(l => l.GetLookupDataAsync(11, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[MakeOption(5, "5")]);
        var sut = CreateSut();
        sut.BatchType = BatchTypeConstants.Tse;
        sut.SubmittedAs = 5;
        _session.Object.ReturnPage = "/Submissions/ViewSubmissions";
        _session.Object.BatchID = 123;

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchDetails", redirect.PageName);
        Assert.Equal(BatchTypeConstants.Tse, _session.Object.BatchType);
        Assert.Null(_session.Object.BatchID);
        Assert.Equal(string.Empty, _session.Object.ReturnPage);
    }

    [Fact]
    public async Task OnPostAsync_PreCassettedCode_SetsRedirectRouteValueMode()
    {
        _lookups.Setup(l => l.GetLookupDataAsync(11, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[MakeOption(5, "5")]);
        var sut = CreateSut();
        sut.SubmittedAs = 5;

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.NotNull(redirect.RouteValues);
        Assert.Equal("create", redirect.RouteValues!["mode"]);
    }
}
