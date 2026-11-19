using Histo.Histology.Interfaces;
using Histo.Histology.Models;
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
    private readonly Mock<IHistologyRefService> _histologyRefs = new();

    private static string Yy => $"{DateTime.Now.Year % 100:D2}";

    public EditAnimalRefModelTests()
    {
        _session.SetupGet(s => s.UserID).Returns(9);
        _histologyRefs.Setup(h => h.GetCountersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRefCounter>)
            [
                new HistologyRefCounter { Type = HistologyRefTypeCode.Neuropath, NextHistologyRef = "10001" },
                new HistologyRefCounter { Type = HistologyRefTypeCode.GeneralPool, NextHistologyRef = "40500" },
            ]);
    }

    private EditAnimalRefModel CreateSut(string original = "", string newSender = "", string newHisto = "") =>
        new(_session.Object, _submissions.Object, _histologyRefs.Object)
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

    [Theory]
    [InlineData("40500")]   // equal to the next counter
    [InlineData("40999")]   // above it
    public async Task SaveHistologyRef_CurrentYearRefAtOrAboveTheCounter_IsRefused(string number)
    {
        var sut = CreateSut("AB1", newHisto: $"{Yy}/{number}");

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal($"The Histology Ref entered is higher than or equal to the current next Histology Ref ({Yy}/40500) for the selected area.", sut.Error);
        Assert.Equal("NewHistologyRef", sut.ErrorField);
        _submissions.Verify(s => s.UpdateAnimalHistologyRefAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveHistologyRef_CurrentYearRefBelowTheCounter_IsSaved()
    {
        var sut = CreateSut("AB1", newHisto: $"{Yy}/40499");

        await sut.OnPostSaveHistologyRefAsync();

        _submissions.Verify(s => s.UpdateAnimalHistologyRefAsync("AB1", $"{Yy}/40499", 9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("25/99999")]   // an earlier year isn't numbered against this year's counters
    [InlineData("99/40999")]   // 1990s
    public async Task SaveHistologyRef_EarlierYearRef_SkipsTheCounterCheck(string histologyRef)
    {
        var sut = CreateSut("AB1", newHisto: histologyRef);

        await sut.OnPostSaveHistologyRefAsync();

        _submissions.Verify(s => s.UpdateAnimalHistologyRefAsync("AB1", histologyRef, 9, It.IsAny<CancellationToken>()), Times.Once);
        _histologyRefs.Verify(h => h.GetCountersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveHistologyRef_CurrentYearRefOutsideEveryTypeRange_IsRefused()
    {
        var sut = CreateSut("AB1", newHisto: $"{Yy}/99999");

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("The Histology Ref entered is outside the number range of every histology type.", sut.Error);
        _submissions.Verify(s => s.UpdateAnimalHistologyRefAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveHistologyRef_NoCounterRowForTheType_IsRefused()
    {
        var sut = CreateSut("AB1", newHisto: $"{Yy}/30001");   // TB Diag has no counter row in the fixture

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("The next available Histology Ref for this type could not be checked.", sut.Error);
        _submissions.Verify(s => s.UpdateAnimalHistologyRefAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveHistologyRef_PgSampleWithTheReversedPgNumber_IsSaved()
    {
        var sut = CreateSut("PG0200/25", newHisto: "25/00200");

        await sut.OnPostSaveHistologyRefAsync();

        _submissions.Verify(s => s.UpdateAnimalHistologyRefAsync("PG0200/25", "25/00200", 9, It.IsAny<CancellationToken>()), Times.Once);
        _histologyRefs.Verify(h => h.GetCountersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("26/40001")]   // a valid ref, but not this PG number's
    [InlineData("")]           // legacy refuses to blank a PG sample's ref
    public async Task SaveHistologyRef_PgSampleWithAnyOtherRef_IsRefused(string histologyRef)
    {
        var sut = CreateSut("PG0200/25", newHisto: histologyRef);

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("The Histology Ref is not correct for the PG Number entered.", sut.Error);
        Assert.Equal("NewHistologyRef", sut.ErrorField);
        _submissions.Verify(s => s.UpdateAnimalHistologyRefAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("PG0200/01")]
    [InlineData("PG0200/85")]
    public async Task SaveHistologyRef_PgSampleFromBefore2002_IsNotChecked(string senderRef)
    {
        var sut = CreateSut(senderRef, newHisto: "26/40001");

        await sut.OnPostSaveHistologyRefAsync();

        _submissions.Verify(s => s.UpdateAnimalHistologyRefAsync(senderRef, "26/40001", 9, It.IsAny<CancellationToken>()), Times.Once);
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
