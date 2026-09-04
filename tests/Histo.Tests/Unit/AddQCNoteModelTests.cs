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

/// <summary>Unit tests for <see cref="AddQCNoteModel"/>.</summary>
public class AddQCNoteModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IQCNoteService> _qc = new();

    public AddQCNoteModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.Setup(s => s.UserID).Returns(99);
    }

    private AddQCNoteModel CreateSut() =>
        new(_session.Object, _qc.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    [Fact]
    public void OnGet_NoBatchIdInSession_RedirectsToIndex()
    {
        _session.Object.BatchID = null;
        var sut = CreateSut();

        var result = sut.OnGet();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public void OnGet_BatchIdInSession_ReturnsPage()
    {
        _session.Object.BatchID = 1;
        var sut = CreateSut();

        var result = sut.OnGet();

        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_NoBatchIdInSession_RedirectsToIndex()
    {
        _session.Object.BatchID = null;
        var sut = CreateSut();

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_AddFails_ReturnsPageWithError()
    {
        _session.Object.BatchID = 1;
        _qc.Setup(q => q.AddAsync(1, 99, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var sut = CreateSut();

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Failed to add the QC note. Please try again.", sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_AddSucceedsWithNoText_SkipsTextUpdate()
    {
        _session.Object.BatchID = 1;
        _qc.Setup(q => q.AddAsync(1, 99, It.IsAny<CancellationToken>())).ReturnsAsync(5);
        var sut = CreateSut();
        sut.Text = "";

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/QC/QCNotes", redirect.PageName);
        _qc.Verify(q => q.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_AddSucceedsWithText_SavesTextAgainstNewNote()
    {
        _session.Object.BatchID = 1;
        _qc.Setup(q => q.AddAsync(1, 99, It.IsAny<CancellationToken>())).ReturnsAsync(5);
        _qc.Setup(q => q.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new QCNote { RowStamp = [0x01] });
        var sut = CreateSut();
        sut.Text = "Some note text";

        var result = await sut.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        _qc.Verify(q => q.UpdateAsync(5, "Some note text", It.IsAny<byte[]>(), 99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_AddSucceedsWithTextButNoRowStamp_SkipsUpdate()
    {
        _session.Object.BatchID = 1;
        _qc.Setup(q => q.AddAsync(1, 99, It.IsAny<CancellationToken>())).ReturnsAsync(5);
        _qc.Setup(q => q.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((QCNote?)null);
        var sut = CreateSut();
        sut.Text = "Some note text";

        var result = await sut.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        _qc.Verify(q => q.UpdateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
