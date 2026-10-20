using System.Security.Claims;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Auth;
using Histo.Web.Pages.Archive;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>A batch ID taken from the URL must be access-checked before any handler runs (red-team IDOR finding).</summary>
public class BatchAccessGuardTests
{
    private const int OwnArea = 4;
    private const int OtherArea = 3;

    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();

    private async Task<(PageHandlerExecutingContext Context, bool NextCalled)> RunAsync(
        int batchId, int batchArea, bool isHistoUser = false, bool isMaintenance = false)
    {
        _session.Setup(s => s.IsHistoUser).Returns(isHistoUser);
        _session.Setup(s => s.IsMaintenance).Returns(isMaintenance);
        _session.Setup(s => s.UserAreaID).Returns(OwnArea);
        _batches.Setup(b => b.GetByIdAsync(batchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Batch { ID = batchId, UserAreaCode = batchArea });

        var identity = new ClaimsIdentity([new Claim(AppClaimTypes.GroupName, "Customer")], "test");
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
            RequestServices = new ServiceCollection().AddSingleton(_batches.Object).BuildServiceProvider(),
        };
        var pageContext = new PageContext(new ActionContext(httpContext, new RouteData(), new PageActionDescriptor()));
        var model = new ArchiveMenuModel(_session.Object) { PageContext = pageContext, BatchId = batchId };
        var context = new PageHandlerExecutingContext(
            pageContext, [], new HandlerMethodDescriptor(), new Dictionary<string, object?>(), model);
        var nextCalled = false;

        await model.OnPageHandlerExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new PageHandlerExecutedContext(pageContext, [], new HandlerMethodDescriptor(), model));
        });

        return (context, nextCalled);
    }

    [Fact]
    public async Task CustomerRequestingABatchFromAnotherArea_IsForbiddenAndTheHandlerDoesNotRun()
    {
        var (context, nextCalled) = await RunAsync(29400, OtherArea);

        Assert.False(nextCalled);
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task CustomerRequestingABatchFromTheirOwnArea_IsAllowedThrough()
    {
        var (context, nextCalled) = await RunAsync(29401, OwnArea);

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task HistoUserAndMaintenance_CanOpenABatchFromAnyArea(bool isHistoUser, bool isMaintenance)
    {
        var (context, nextCalled) = await RunAsync(29402, OtherArea, isHistoUser, isMaintenance);

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }
}
