using Histo.Administration.Interfaces;
using Histo.Submissions.Interfaces;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="ViewImportedDataModel.BackLinkPage"/> — open-redirect guard.</summary>
public class ViewImportedDataModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IUrlHelper> _url = new();

    private ViewImportedDataModel CreateSut() =>
        new(_session.Object, _lookups.Object, _submissions.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new DefaultHttpContext(),
            },
            Url = _url.Object,
        };

    [Fact]
    public void BackLinkPage_NoReturnPage_FallsBackToSearchMenu()
    {
        var sut = CreateSut();

        Assert.Equal("/Search/SearchMenu", sut.BackLinkPage);
    }

    [Fact]
    public void BackLinkPage_LocalReturnPageWithQueryString_IsUsedAsIs()
    {
        // The "View Old ICC_Sub data" link from SearchBlockRefs passes a ReturnPage that already
        // has its own query string embedded — must round-trip unchanged, not just a bare path.
        const string returnPage = "/Search/SearchBlockRefs?SenderRef=S1&Submitted=true";
        _url.Setup(u => u.IsLocalUrl(returnPage)).Returns(true);
        var sut = CreateSut();
        sut.ReturnPage = returnPage;

        Assert.Equal(returnPage, sut.BackLinkPage);
    }

    [Fact]
    public void BackLinkPage_NonLocalReturnPage_FallsBackToSearchMenu()
    {
        const string external = "https://evil.example.com";
        _url.Setup(u => u.IsLocalUrl(external)).Returns(false);
        var sut = CreateSut();
        sut.ReturnPage = external;

        Assert.Equal("/Search/SearchMenu", sut.BackLinkPage);
    }
}
