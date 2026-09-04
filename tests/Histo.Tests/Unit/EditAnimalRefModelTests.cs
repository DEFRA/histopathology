using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Admin;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="EditAnimalRefModel"/> — the per-animal Sender Ref /
/// Histology Ref rename utility (resolves ISS-022).
/// </summary>
public class EditAnimalRefModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();

    public EditAnimalRefModelTests()
    {
        _session.Setup(s => s.UserID).Returns(99);
    }

    private EditAnimalRefModel CreateSut() =>
        new(_session.Object, _submissions.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    // ── OnPostGetHistologyRefAsync ───────────────────────────────────────────

    [Fact]
    public async Task OnPostGetHistologyRefAsync_EmptySenderRef_ReturnsError()
    {
        var sut = CreateSut();
        sut.OriginalSenderRef = "";

        await sut.OnPostGetHistologyRefAsync();

        Assert.Equal("You must enter a Sample Ref.", sut.Error);
    }

    [Fact]
    public async Task OnPostGetHistologyRefAsync_SenderRefNotFound_ReturnsError()
    {
        _submissions.Setup(s => s.GetAnimalBySenderAsync("S123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[]);
        var sut = CreateSut();
        sut.OriginalSenderRef = "S123";

        await sut.OnPostGetHistologyRefAsync();

        Assert.Equal("Sample Ref. not found.", sut.Error);
    }

    [Fact]
    public async Task OnPostGetHistologyRefAsync_FoundWithHistologyRef_SetsCurrentHistologyRef()
    {
        _submissions.Setup(s => s.GetAnimalBySenderAsync("S123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[new SenderSearchResult { ID = 1, SenderRef = "S123", HistologyRef = "24/00123" }]);
        var sut = CreateSut();
        sut.OriginalSenderRef = "S123";

        await sut.OnPostGetHistologyRefAsync();

        Assert.Equal("24/00123", sut.CurrentHistologyRef);
        Assert.Null(sut.Error);
    }

    [Fact]
    public async Task OnPostGetHistologyRefAsync_FoundWithNoHistologyRef_SetsNullPlaceholder()
    {
        _submissions.Setup(s => s.GetAnimalBySenderAsync("S123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[new SenderSearchResult { ID = 1, SenderRef = "S123", HistologyRef = null }]);
        var sut = CreateSut();
        sut.OriginalSenderRef = "S123";

        await sut.OnPostGetHistologyRefAsync();

        Assert.Equal("<null>", sut.CurrentHistologyRef);
    }

    // ── OnPostEditSenderRefAsync ──────────────────────────────────────────────

    [Fact]
    public async Task OnPostEditSenderRefAsync_EmptyOriginalSenderRef_ReturnsError()
    {
        var sut = CreateSut();
        sut.OriginalSenderRef = "";
        sut.NewSenderRef = "S456";

        await sut.OnPostEditSenderRefAsync();

        Assert.Equal("You must enter a Sample Ref.", sut.Error);
    }

    [Fact]
    public async Task OnPostEditSenderRefAsync_EmptyNewSenderRef_ReturnsError()
    {
        var sut = CreateSut();
        sut.OriginalSenderRef = "S123";
        sut.NewSenderRef = "";

        await sut.OnPostEditSenderRefAsync();

        Assert.Equal("You must enter a New Sample Ref.", sut.Error);
    }

    [Fact]
    public async Task OnPostEditSenderRefAsync_Success_SetsSuccessMessage()
    {
        _submissions.Setup(s => s.UpdateAnimalSenderRefAsync("S123", "S456", 99, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = CreateSut();
        sut.OriginalSenderRef = "S123";
        sut.NewSenderRef = "S456";

        await sut.OnPostEditSenderRefAsync();

        Assert.Equal("The new Sample Ref has been saved", sut.SuccessMessage);
        Assert.Null(sut.Error);
    }

    [Fact]
    public async Task OnPostEditSenderRefAsync_AnimalRefUpdateException_SetsError()
    {
        _submissions.Setup(s => s.UpdateAnimalSenderRefAsync("S123", "S456", 99, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AnimalRefUpdateException("New Sample Ref already exists"));
        var sut = CreateSut();
        sut.OriginalSenderRef = "S123";
        sut.NewSenderRef = "S456";

        await sut.OnPostEditSenderRefAsync();

        Assert.Equal("The Sample Ref was not updated because: New Sample Ref already exists", sut.Error);
    }

    [Fact]
    public async Task OnPostEditSenderRefAsync_UnexpectedException_SetsGenericError()
    {
        _submissions.Setup(s => s.UpdateAnimalSenderRefAsync("S123", "S456", 99, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB timeout"));
        var sut = CreateSut();
        sut.OriginalSenderRef = "S123";
        sut.NewSenderRef = "S456";

        await sut.OnPostEditSenderRefAsync();

        Assert.Equal("ERROR: DB timeout", sut.Error);
    }

    // ── OnPostSaveHistologyRefAsync ───────────────────────────────────────────

    [Fact]
    public async Task OnPostSaveHistologyRefAsync_EmptySenderRef_ReturnsError()
    {
        var sut = CreateSut();
        sut.OriginalSenderRef = "";

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("You must enter a Sample Ref.", sut.Error);
    }

    [Fact]
    public async Task OnPostSaveHistologyRefAsync_PgNumberWithWrongHistologyRef_ReturnsError()
    {
        var sut = CreateSut();
        sut.OriginalSenderRef = "PG012302";
        sut.NewHistologyRef = "99/99999";

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("The Histology Ref is not correct for the PG Number entered.", sut.Error);
    }

    [Fact]
    public async Task OnPostSaveHistologyRefAsync_PgNumberWithCorrectHistologyRef_Succeeds()
    {
        _submissions.Setup(s => s.UpdateAnimalHistologyRefAsync("PG012302", "02/00123", 99, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = CreateSut();
        sut.OriginalSenderRef = "PG012302";
        sut.NewHistologyRef = "02/00123";

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("The new Histology Ref has been saved", sut.SuccessMessage);
    }

    [Fact]
    public async Task OnPostSaveHistologyRefAsync_InvalidHistologyRefFormat_ReturnsError()
    {
        var sut = CreateSut();
        sut.OriginalSenderRef = "S123";
        sut.NewHistologyRef = "notavalidref";

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("You must enter a valid Histology Ref.", sut.Error);
    }

    [Fact]
    public async Task OnPostSaveHistologyRefAsync_ValidHistologyRef_SetsSuccessMessage()
    {
        _submissions.Setup(s => s.UpdateAnimalHistologyRefAsync("S123", "24/00123", 99, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = CreateSut();
        sut.OriginalSenderRef = "S123";
        sut.NewHistologyRef = "24/00123";

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("The new Histology Ref has been saved", sut.SuccessMessage);
        Assert.Null(sut.Error);
    }

    [Fact]
    public async Task OnPostSaveHistologyRefAsync_EmptyNewHistologyRef_ClearsRefAndSetsRemovalMessage()
    {
        _submissions.Setup(s => s.UpdateAnimalHistologyRefAsync("S123", "", 99, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = CreateSut();
        sut.OriginalSenderRef = "S123";
        sut.NewHistologyRef = "";

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("The old Histology Ref has been removed. You may now enter a new Histology Ref.", sut.SuccessMessage);
    }

    [Fact]
    public async Task OnPostSaveHistologyRefAsync_AnimalRefUpdateException_SetsError()
    {
        _submissions.Setup(s => s.UpdateAnimalHistologyRefAsync("S123", "24/00123", 99, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AnimalRefUpdateException("Histology Ref already in use"));
        var sut = CreateSut();
        sut.OriginalSenderRef = "S123";
        sut.NewHistologyRef = "24/00123";

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("The Histology Reference was not updated because: Histology Ref already in use", sut.Error);
    }
}
