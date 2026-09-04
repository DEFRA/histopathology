using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="SearchSampleModel"/>.</summary>
public class SearchSampleModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();

    private SearchSampleModel CreateSut() =>
        new(_session.Object, _submissions.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    [Fact]
    public async Task OnPostAsync_BlankSenderRef_DoesNotSearch()
    {
        var sut = CreateSut();
        sut.SenderRef = "   ";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Empty(sut.Results);
        _submissions.Verify(s => s.GetAnimalsBySenderRefAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_SenderRefProvided_ReturnsResults()
    {
        _submissions.Setup(s => s.GetAnimalsBySenderRefAsync("S123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[new SenderSearchResult { ID = 1, SenderRef = "S123" }]);
        var sut = CreateSut();
        sut.SenderRef = "S123";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Single(sut.Results);
    }
}
