using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="ViewSamplesModel"/>'s "other ref" resolution — legacy source:
/// <c>ViewSamples.aspx.vb::FillviewGrid</c>. The resolved counterpart ref is shown ONLY as a
/// read-only label (<see cref="ViewSamplesModel.OtherFieldLabel"/>); the SenderRef/HistologyRef
/// input boxes themselves are never auto-populated and always reflect exactly what was submitted.
/// </summary>
public class ViewSamplesModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<ILookupService> _lookups = new();

    public ViewSamplesModelTests()
    {
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
    }

    private ViewSamplesModel CreateSut() =>
        new(_session.Object, _submissions.Object, _lookups.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
        };

    [Fact]
    public async Task OnGetAsync_SearchBySenderRef_ShowsHistologyRefLabel_InputFieldUnchanged()
    {
        _submissions.Setup(s => s.GetAnimalTissuesAsync("S1", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AnimalTissueSearchResult>)[new AnimalTissueSearchResult { SenderRef = "S1", HistologyRef = "H1" }]);
        var sut = CreateSut();
        sut.Submitted = true;
        sut.SenderRef = "S1";

        await sut.OnGetAsync();

        Assert.Equal("Histology Ref: H1", sut.OtherFieldLabel);
        Assert.Null(sut.HistologyRef);
        Assert.Equal("S1", sut.SenderRef);
    }

    [Fact]
    public async Task OnGetAsync_ChangingSenderRef_RefreshesHistologyRefLabel()
    {
        _submissions.Setup(s => s.GetAnimalTissuesAsync("S2", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AnimalTissueSearchResult>)[new AnimalTissueSearchResult { SenderRef = "S2", HistologyRef = "H2" }]);
        var sut = CreateSut();
        sut.Submitted = true;
        sut.SenderRef = "S2";

        await sut.OnGetAsync();

        Assert.Equal("Histology Ref: H2", sut.OtherFieldLabel);
        Assert.Null(sut.HistologyRef);
    }

    [Fact]
    public async Task OnGetAsync_SearchByHistologyRef_ShowsSenderRefLabel_InputFieldUnchanged()
    {
        _submissions.Setup(s => s.GetAnimalTissuesAsync(null, "H1", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AnimalTissueSearchResult>)[new AnimalTissueSearchResult { SenderRef = "S1", HistologyRef = "H1" }]);
        var sut = CreateSut();
        sut.Submitted = true;
        sut.HistologyRef = "H1";

        await sut.OnGetAsync();

        Assert.Equal("Sender Ref: S1", sut.OtherFieldLabel);
        Assert.Null(sut.SenderRef);
        Assert.Equal("H1", sut.HistologyRef);
    }

    [Fact]
    public async Task OnGetAsync_ChangingHistologyRef_RefreshesSenderRefLabel()
    {
        _submissions.Setup(s => s.GetAnimalTissuesAsync(null, "H2", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AnimalTissueSearchResult>)[new AnimalTissueSearchResult { SenderRef = "S2", HistologyRef = "H2" }]);
        var sut = CreateSut();
        sut.Submitted = true;
        sut.HistologyRef = "H2";

        await sut.OnGetAsync();

        Assert.Equal("Sender Ref: S2", sut.OtherFieldLabel);
        Assert.Null(sut.SenderRef);
    }

    [Fact]
    public async Task OnGetAsync_BothRefsGivenTogether_SetsValidationError()
    {
        var sut = CreateSut();
        sut.Submitted = true;
        sut.SenderRef = "S1";
        sut.HistologyRef = "H9";

        await sut.OnGetAsync();

        Assert.True(sut.Errors.ContainsKey(nameof(sut.SenderRef)));
        Assert.False(sut.Searched);
        _submissions.Verify(s => s.GetAnimalTissuesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_NeitherRefProvided_SetsValidationError()
    {
        var sut = CreateSut();
        sut.Submitted = true;

        await sut.OnGetAsync();

        Assert.True(sut.Errors.ContainsKey(nameof(sut.SenderRef)));
    }
}

