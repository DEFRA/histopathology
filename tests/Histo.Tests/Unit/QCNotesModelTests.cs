using Histo.QualityControl.Interfaces;
using Histo.QualityControl.Models;
using Histo.Web.Pages.QC;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="QCNotesModel"/>.</summary>
public class QCNotesModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IQCNoteService> _qc = new();

    public QCNotesModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
    }

    private QCNotesModel CreateSut() =>
        new(_session.Object, _qc.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    [Fact]
    public async Task OnGetAsync_LoadsAllNotes()
    {
        _qc.Setup(q => q.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<QCNote>)[new QCNote { QCNoteRef = 1 }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Single(sut.Notes);
    }

    [Fact]
    public void IsGlobalView_NoBatchIdInSession_IsTrue()
    {
        _session.Object.BatchID = null;
        var sut = CreateSut();

        Assert.True(sut.IsGlobalView);
    }

    [Fact]
    public void IsGlobalView_ZeroBatchIdInSession_IsTrue()
    {
        _session.Object.BatchID = 0;
        var sut = CreateSut();

        Assert.True(sut.IsGlobalView);
    }

    [Fact]
    public void IsGlobalView_PositiveBatchIdInSession_IsFalse()
    {
        _session.Object.BatchID = 5;
        var sut = CreateSut();

        Assert.False(sut.IsGlobalView);
    }

    [Fact]
    public void OnPostEdit_RedirectsToEditQCNoteWithNoteId()
    {
        var sut = CreateSut();

        var result = sut.OnPostEdit(7);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/QC/EditQCNote", redirect.PageName);
        Assert.Equal(7, redirect.RouteValues!["noteId"]);
    }

    [Fact]
    public void OnPostGoAsync_QuickGoRefSet_RedirectsToEditQCNote()
    {
        var sut = CreateSut();
        sut.QuickGoRef = 9;

        var result = sut.OnPostGoAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/QC/EditQCNote", redirect.PageName);
        Assert.Equal(9, redirect.RouteValues!["noteId"]);
    }

    [Fact]
    public void OnPostGoAsync_NoQuickGoRef_RedirectsToSelf()
    {
        var sut = CreateSut();
        sut.QuickGoRef = null;

        var result = sut.OnPostGoAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Null(redirect.PageName);
    }
}
