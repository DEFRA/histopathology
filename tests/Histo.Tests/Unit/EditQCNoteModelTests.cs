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

/// <summary>Unit tests for <see cref="EditQCNoteModel"/>.</summary>
public class EditQCNoteModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IQCNoteService> _qc = new();

    public EditQCNoteModelTests()
    {
        _session.SetupGet(s => s.UserID).Returns(9);
    }

    private EditQCNoteModel CreateSut() =>
        new(_session.Object, _qc.Object)
        {
            NoteId = 4,
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public async Task OnGetAsync_NoteMissing_RedirectsToQcNotes()
    {
        _qc.Setup(q => q.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync((QCNote?)null);
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.Equal("/QC/QCNotes", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.Equal("Edit QC Note", sut.ViewData["Title"]);
        Assert.Equal("Edit QC Note", sut.ViewData["PageTitle"]);
    }

    [Fact]
    public async Task OnGetAsync_NoteFound_PopulatesTextAndReturnsPage()
    {
        _qc.Setup(q => q.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(new QCNote { ID = 1, Text = "existing" });
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("existing", sut.Text);
        Assert.NotNull(sut.Note);
    }

    [Fact]
    public async Task OnPostAsync_NoteMissingRowStamp_RedirectsWithoutUpdating()
    {
        _qc.Setup(q => q.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(new QCNote { ID = 1 });
        var sut = CreateSut();

        var result = await sut.OnPostAsync();

        Assert.Equal("/QC/QCNotes", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.Equal("Edit QC Note", sut.ViewData["Title"]);
        Assert.Equal("Edit QC Note", sut.ViewData["PageTitle"]);
        _qc.Verify(q => q.UpdateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ValidNote_UpdatesAndRedirects()
    {
        byte[] stamp = [7];
        _qc.Setup(q => q.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(new QCNote { ID = 1, RowStamp = stamp });
        var sut = CreateSut();
        sut.Text = "updated";

        var result = await sut.OnPostAsync();

        Assert.Equal("/QC/QCNotes", Assert.IsType<RedirectToPageResult>(result).PageName);
        _qc.Verify(q => q.UpdateAsync(4, "updated", stamp, 9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ConcurrentEdit_ReturnsPageWithConcurrencyError()
    {
        byte[] stamp = [7];
        _qc.Setup(q => q.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(new QCNote { ID = 1, RowStamp = stamp });
        _qc.Setup(q => q.UpdateAsync(4, It.IsAny<string>(), stamp, 9, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new QCNoteConcurrencyException());
        var sut = CreateSut();

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Another user has modified this QC note. Please reload and try again.", sut.ConcurrencyError);
    }
}
