using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Submissions;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="ViewSubmissionsModel"/>. Confirmed live: legacy's <c>ViewSubmissions</c>
/// has no "Submitted area" filter and does not restrict results by the logged-in user's area — a
/// previous fix in this repo wrongly added both; this regression test guards against reintroducing
/// either.
/// </summary>
public class ViewSubmissionsModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ILookupService> _lookups = new();

    public ViewSubmissionsModelTests()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSearchResult>)[]);
    }

    private ViewSubmissionsModel CreateSut() =>
        new(_session.Object, _batches.Object, _users.Object, _lookups.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
        };

    [Fact]
    public async Task OnGetAsync_NeverAppliesAreaRestriction_RegardlessOfCallerRole()
    {
        _session.Setup(s => s.IsHistoUser).Returns(false);
        _session.Setup(s => s.IsMaintenance).Returns(false);
        _session.Setup(s => s.UserAreaID).Returns(7);
        var sut = CreateSut();

        await sut.OnGetAsync();

        _batches.Verify(b => b.SearchAsync(
            It.Is<BatchSearchCriteria>(c => c.SubmittedArea == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void HasNoSubmittedAreaOrAreaRestrictionProperty()
    {
        var modelType = typeof(ViewSubmissionsModel);
        Assert.Null(modelType.GetProperty("SubmittedArea"));
        Assert.Null(modelType.GetProperty("IsAreaRestricted"));
    }
}
