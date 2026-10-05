using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Verifies <see cref="CassettedModel"/>'s TSE/Non-TSE radio always defaults to TSE on a fresh
/// visit, regardless of what a previous, unrelated submission left in <c>Session.BatchType</c>.
/// </summary>
public class CassettedModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();

    public CassettedModelTests() =>
        _lookups.Setup(l => l.GetLookupDataAsync(11, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);

    private CassettedModel CreateSut() =>
        new(_session.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
        };

    [Fact]
    public async Task OnGetAsync_DefaultsToTse_EvenWhenSessionHasNonTseFromAnEarlierSubmission()
    {
        // Regression: a previously completed/abandoned Non-TSE submission left Session.BatchType
        // set to Non-TSE — a brand-new visit (not restore) must still default to TSE.
        _session.Setup(s => s.BatchType).Returns(BatchTypeConstants.NonTse);
        var sut = CreateSut();

        await sut.OnGetAsync(restore: false);

        Assert.Equal(BatchTypeConstants.Tse, sut.BatchType);
    }

    [Fact]
    public async Task OnGetAsync_Restore_UsesCassettedBatchTypeDraft_NotTheSharedBatchTypeField()
    {
        // Back from BatchDetails mid-journey (restore=true) must restore the just-made choice from
        // the journey-exclusive draft field — never the shared Session.BatchType, which could have
        // been overwritten by an unrelated existing batch opened elsewhere in the same session.
        _session.SetupProperty(s => s.CassettedBatchTypeDraft, BatchTypeConstants.NonTse);
        _session.Setup(s => s.BatchType).Returns(BatchTypeConstants.Tse);
        var sut = CreateSut();

        await sut.OnGetAsync(restore: true);

        Assert.Equal(BatchTypeConstants.NonTse, sut.BatchType);
    }

    [Fact]
    public async Task OnGetAsync_NotRestoring_ClearsAnyLeftoverDraft()
    {
        _session.SetupProperty(s => s.CassettedBatchTypeDraft, BatchTypeConstants.NonTse);
        var sut = CreateSut();

        await sut.OnGetAsync(restore: false);

        Assert.Equal(BatchTypeConstants.Tse, sut.BatchType);
        Assert.Null(sut.Session.CassettedBatchTypeDraft);
    }
}
