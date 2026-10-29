using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Admin;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="EditAnimalRefModel"/>.</summary>
public class EditAnimalRefModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();

    public EditAnimalRefModelTests()
    {
        _session.SetupGet(s => s.UserID).Returns(9);
    }

    private EditAnimalRefModel CreateSut(string original = "", string newSender = "", string newHisto = "") =>
        new(_session.Object, _submissions.Object)
        {
            OriginalSenderRef = original,
            NewSenderRef = newSender,
            NewHistologyRef = newHisto,
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public void OnGet_SetsTitles()
    {
        var sut = CreateSut();

        sut.OnGet();

        Assert.Equal("Edit Sender/Histology Ref", sut.ViewData["Title"]);
        Assert.Equal("Edit Sender/Histology Ref", sut.ViewData["PageTitle"]);
    }

    [Fact]
    public async Task GetHistologyRef_BlankSenderRef_ReturnsPageWithFieldError()
    {
        var sut = CreateSut(original: " ");

        var result = await sut.OnPostGetHistologyRefAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("You must enter a Sample Ref.", sut.Error);
        Assert.Equal("OriginalSenderRef", sut.ErrorField);
    }

    [Fact]
    public async Task GetHistologyRef_NoMatch_ReportsNotFound()
    {
        _submissions.Setup(s => s.GetAnimalBySenderAsync("AB1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[]);
        var sut = CreateSut(original: " AB1 ");

        await sut.OnPostGetHistologyRefAsync();

        Assert.Equal("Sample Ref. not found.", sut.Error);
        Assert.Equal("OriginalSenderRef", sut.ErrorField);
        Assert.Equal("AB1", sut.OriginalSenderRef);
    }

    [Fact]
    public async Task GetHistologyRef_Match_ExposesCurrentHistologyRef()
    {
        _submissions.Setup(s => s.GetAnimalBySenderAsync("AB1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[new SenderSearchResult { ID = 1, SenderRef = "AB1", HistologyRef = "26/40001" }]);
        var sut = CreateSut(original: "AB1");

        await sut.OnPostGetHistologyRefAsync();

        Assert.Equal("26/40001", sut.CurrentHistologyRef);
        Assert.Null(sut.Error);
    }

    [Fact]
    public async Task EditSenderRef_BlankOriginal_ReturnsOriginalFieldError()
    {
        var sut = CreateSut(newSender: "NEW1");

        await sut.OnPostEditSenderRefAsync();

        Assert.Equal("You must enter a Sample Ref.", sut.Error);
        Assert.Equal("OriginalSenderRef", sut.ErrorField);
    }

    [Fact]
    public async Task EditSenderRef_BlankNewRef_ReturnsNewFieldError()
    {
        var sut = CreateSut(original: "AB1");

        await sut.OnPostEditSenderRefAsync();

        Assert.Equal("You must enter a New Sample Ref.", sut.Error);
        Assert.Equal("NewSenderRef", sut.ErrorField);
    }

    [Fact]
    public async Task EditSenderRef_Valid_UpdatesWithTrimmedValuesAndReportsSuccess()
    {
        var sut = CreateSut(" AB1 ", " NEW1 ");

        await sut.OnPostEditSenderRefAsync();

        _submissions.Verify(s => s.UpdateAnimalSenderRefAsync("AB1", "NEW1", 9, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("The new Sample Ref has been saved", sut.SuccessMessage);
    }

    [Fact]
    public async Task EditSenderRef_DomainFailure_ReportsReason()
    {
        _submissions.Setup(s => s.UpdateAnimalSenderRefAsync("AB1", "NEW1", 9, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AnimalRefUpdateException("already exists"));
        var sut = CreateSut("AB1", "NEW1");

        await sut.OnPostEditSenderRefAsync();

        Assert.Equal("The Sample Ref was not updated because: already exists", sut.Error);
        Assert.Null(sut.SuccessMessage);
    }

    [Fact]
    public async Task EditSenderRef_UnexpectedFailure_ReportsError()
    {
        _submissions.Setup(s => s.UpdateAnimalSenderRefAsync("AB1", "NEW1", 9, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var sut = CreateSut("AB1", "NEW1");

        await sut.OnPostEditSenderRefAsync();

        Assert.Equal("ERROR: boom", sut.Error);
    }

    [Fact]
    public async Task SaveHistologyRef_BlankOriginal_ReturnsOriginalFieldError()
    {
        var sut = CreateSut(newHisto: "26/40001");

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("You must enter a Sample Ref.", sut.Error);
        Assert.Equal("OriginalSenderRef", sut.ErrorField);
    }

    [Fact]
    public async Task SaveHistologyRef_InvalidFormat_ReturnsFieldError()
    {
        var sut = CreateSut("AB1", newHisto: "not-a-ref");

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("You must enter a valid Histology Ref.", sut.Error);
        Assert.Equal("NewHistologyRef", sut.ErrorField);
        _submissions.Verify(s => s.UpdateAnimalHistologyRefAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveHistologyRef_ValidRef_SavesAndReportsSuccess()
    {
        var sut = CreateSut("AB1", newHisto: " 26/40001 ");

        await sut.OnPostSaveHistologyRefAsync();

        _submissions.Verify(s => s.UpdateAnimalHistologyRefAsync("AB1", "26/40001", 9, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("The new Histology Ref has been saved", sut.SuccessMessage);
    }

    [Fact]
    public async Task SaveHistologyRef_BlankRef_RemovesExistingRef()
    {
        var sut = CreateSut("AB1");

        await sut.OnPostSaveHistologyRefAsync();

        _submissions.Verify(s => s.UpdateAnimalHistologyRefAsync("AB1", string.Empty, 9, It.IsAny<CancellationToken>()), Times.Once);
        Assert.StartsWith("The old Histology Ref has been removed.", sut.SuccessMessage);
    }

    [Fact]
    public async Task SaveHistologyRef_DomainFailure_ReportsReason()
    {
        _submissions.Setup(s => s.UpdateAnimalHistologyRefAsync("AB1", string.Empty, 9, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AnimalRefUpdateException("in use"));
        var sut = CreateSut("AB1");

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("The Histology Reference was not updated because: in use", sut.Error);
    }

    [Fact]
    public async Task SaveHistologyRef_UnexpectedFailure_ReportsError()
    {
        _submissions.Setup(s => s.UpdateAnimalHistologyRefAsync("AB1", string.Empty, 9, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var sut = CreateSut("AB1");

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("ERROR: boom", sut.Error);
    }
}
