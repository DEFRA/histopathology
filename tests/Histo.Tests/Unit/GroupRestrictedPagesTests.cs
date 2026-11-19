using System.Security.Claims;
using Histo.Web.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace Histo.Tests.Unit;

/// <summary>Legacy <c>CheckPermissions()</c>: Histo-only pages must not open for Customer, even from a browser-history link.</summary>
public class GroupRestrictedPagesTests
{
    private static async Task<(PageHandlerExecutingContext Context, bool NextCalled)> RunAsync(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var pageContext = new PageContext(new ActionContext(httpContext, new RouteData(), new PageActionDescriptor()));
        var context = new PageHandlerExecutingContext(
            pageContext, [], new HandlerMethodDescriptor(), new Dictionary<string, object?>(), handlerInstance: new object());
        var nextCalled = false;

        await new HistoOrMaintenancePageFilter().OnPageHandlerExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new PageHandlerExecutedContext(pageContext, [], new HandlerMethodDescriptor(), new object()));
        });

        return (context, nextCalled);
    }

    private static ClaimsPrincipal SignedIn(string? group)
    {
        var identity = new ClaimsIdentity("test");
        if (group is not null) identity.AddClaim(new Claim(AppClaimTypes.GroupName, group));
        return new ClaimsPrincipal(identity);
    }

    [Theory]
    [InlineData("Histopathology User")]
    [InlineData("Maintenance")]
    public async Task HistoUserAndMaintenance_AreAllowedThrough(string group)
    {
        var (context, nextCalled) = await RunAsync(SignedIn(group));

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Unknown group")]
    [InlineData(null)]
    public async Task OtherGroups_AreSentToHomeAndTheHandlerDoesNotRun(string? group)
    {
        var (context, nextCalled) = await RunAsync(SignedIn(group));

        Assert.False(nextCalled);
        var redirect = Assert.IsType<RedirectToPageResult>(context.Result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task AnonymousUser_IsChallenged()
    {
        var (context, nextCalled) = await RunAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.False(nextCalled);
        Assert.IsType<ChallengeResult>(context.Result);
    }

    [Fact]
    public void ProgramRegistersTheGroupRestrictions()
    {
        var program = ReadSource("src", "Histo.Web", "Program.cs");

        Assert.Contains("GroupRestrictedPages.Apply(options)", program);
    }

    [Fact]
    public void EveryRestrictedPageExistsAndIsListedOnce()
    {
        var all = GroupRestrictedPages.HistoOrMaintenance.Concat(GroupRestrictedPages.MaintenanceOnly).ToList();

        Assert.Equal(all.Count, all.Distinct().Count());
        foreach (var page in all)
            Assert.True(SourceExists("src", "Histo.Web", "Pages", page.TrimStart('/').Replace('/', Path.DirectorySeparatorChar) + ".cshtml"), $"{page} has no Razor page.");
    }

    [Theory]
    [InlineData("/Batches/Cassetted")]
    [InlineData("/Submissions/ViewSubmissions")]
    [InlineData("/Search/SearchPMDates")]
    [InlineData("/Search/ViewSamples")]
    public void PagesCustomersUseAreNotRestricted(string page)
    {
        Assert.DoesNotContain(page, GroupRestrictedPages.HistoOrMaintenance);
        Assert.DoesNotContain(page, GroupRestrictedPages.MaintenanceOnly);
    }

    [Theory]
    [InlineData("/QC/QualityData")]
    [InlineData("/Archive/ArchiveTissues")]
    [InlineData("/Batches/BatchesForDispatch")]
    [InlineData("/Search/SearchMenu")]
    [InlineData("/Batches/EditSubmissionStatus")]
    [InlineData("/QC/AddQCNote")]
    [InlineData("/QC/EditQualityDataTest")]
    public void HistoOnlyPagesAreRestricted(string page) =>
        Assert.Contains(page, GroupRestrictedPages.HistoOrMaintenance);

    private static bool SourceExists(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine([dir.FullName, .. parts])))
            dir = dir.Parent;
        return dir != null;
    }

    private static string ReadSource(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine([dir.FullName, .. parts])))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine([dir!.FullName, .. parts]));
    }
}
