using Histo.QualityControl.Interfaces;
using Histo.QualityControl.Models;
using Histo.Web.Pages.QC;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="AddQCNoteModel"/>.</summary>
public class AddQCNoteModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IQCNoteService> _qc = new();

    public AddQCNoteModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupGet(s => s.UserID).Returns(9);
    }

    private AddQCNoteModel CreateSut() =>
        new(_session.Object, _qc.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public void OnGet_NoBatchInSession_RedirectsToIndexAndSetsTitles()
    {
        _session.Object.BatchID = null;
        var sut = CreateSut();

        var result = sut.OnGet();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
        Assert.Equal("Add QC Note", sut.ViewData["Title"]);
        Assert.Equal("Add QC Note", sut.ViewData["PageTitle"]);
    }

    [Fact]
    public void OnGet_BatchInSession_ReturnsPage()
    {
        _session.Object.BatchID = 5;
        var sut = CreateSut();

        var result = sut.OnGet();

        Assert.IsType<PageResult>(result);
        Assert.Equal(5, sut.BatchID);
    }

    [Fact]
    public async Task OnPostAsync_NoBatchInSession_RedirectsToIndexWithoutCreatingNote()
    {
        _session.Object.BatchID = null;
        var sut = CreateSut();

        var result = await sut.OnPostAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.Equal("Add QC Note", sut.ViewData["Title"]);
        Assert.Equal("Add QC Note", sut.ViewData["PageTitle"]);
        _qc.Verify(q => q.AddAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_AddFails_ReturnsPageWithError()
    {
        _session.Object.BatchID = 5;
        _qc.Setup(q => q.AddAsync(5, 9, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var sut = CreateSut();

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Failed to add the QC note. Please try again.", sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_BlankText_CreatesNoteWithoutUpdatingText()
    {
        _session.Object.BatchID = 5;
        _qc.Setup(q => q.AddAsync(5, 9, It.IsAny<CancellationToken>())).ReturnsAsync(11);
        var sut = CreateSut();
        sut.Text = "  ";

        var result = await sut.OnPostAsync();

        Assert.Equal("/QC/QCNotes", Assert.IsType<RedirectToPageResult>(result).PageName);
        _qc.Verify(q => q.UpdateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_TextEntered_SavesTextAgainstNewNote()
    {
        _session.Object.BatchID = 5;
        byte[] stamp = [1, 2, 3];
        _qc.Setup(q => q.AddAsync(5, 9, It.IsAny<CancellationToken>())).ReturnsAsync(11);
        _qc.Setup(q => q.GetByIdAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(new QCNote { ID = 5, RowStamp = stamp });
        var sut = CreateSut();
        sut.Text = "Stain was weak";

        var result = await sut.OnPostAsync();

        Assert.Equal("/QC/QCNotes", Assert.IsType<RedirectToPageResult>(result).PageName);
        _qc.Verify(q => q.UpdateAsync(11, "Stain was weak", stamp, 9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_TextEnteredButNoteHasNoRowStamp_SkipsUpdate()
    {
        _session.Object.BatchID = 5;
        _qc.Setup(q => q.AddAsync(5, 9, It.IsAny<CancellationToken>())).ReturnsAsync(11);
        _qc.Setup(q => q.GetByIdAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(new QCNote { ID = 5 });
        var sut = CreateSut();
        sut.Text = "Stain was weak";

        var result = await sut.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        _qc.Verify(q => q.UpdateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
