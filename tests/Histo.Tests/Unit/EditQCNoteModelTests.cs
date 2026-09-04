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

/// <summary>Unit tests for <see cref="EditQCNoteModel"/>.</summary>
public class EditQCNoteModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IQCNoteService> _qc = new();

    public EditQCNoteModelTests()
    {
        _session.Setup(s => s.UserID).Returns(99);
    }

    private EditQCNoteModel CreateSut() =>
        new(_session.Object, _qc.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    [Fact]
    public async Task OnGetAsync_NoteNotFound_RedirectsToQCNotes()
    {
        _qc.Setup(q => q.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((QCNote?)null);
        var sut = CreateSut();
        sut.NoteId = 1;

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/QC/QCNotes", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_NoteFound_PopulatesText()
    {
        _qc.Setup(q => q.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new QCNote { Text = "Existing text" });
        var sut = CreateSut();
        sut.NoteId = 1;

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Existing text", sut.Text);
    }

    [Fact]
    public async Task OnPostAsync_NoteHasNoRowStamp_RedirectsToQCNotes()
    {
        _qc.Setup(q => q.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new QCNote { RowStamp = null });
        var sut = CreateSut();
        sut.NoteId = 1;

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/QC/QCNotes", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_Success_RedirectsToQCNotes()
    {
        _qc.Setup(q => q.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new QCNote { RowStamp = [0x01] });
        var sut = CreateSut();
        sut.NoteId = 1;
        sut.Text = "Updated text";

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/QC/QCNotes", redirect.PageName);
        _qc.Verify(q => q.UpdateAsync(1, "Updated text", It.IsAny<byte[]>(), 99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ConcurrencyException_ReturnsPageWithError()
    {
        _qc.Setup(q => q.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new QCNote { RowStamp = [0x01] });
        _qc.Setup(q => q.UpdateAsync(1, It.IsAny<string>(), It.IsAny<byte[]>(), 99, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new QCNoteConcurrencyException());
        var sut = CreateSut();
        sut.NoteId = 1;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Another user has modified this QC note. Please reload and try again.", sut.ConcurrencyError);
    }
}
