using System.Security.Claims;
using Histo.Administration.Interfaces;
using Histo.Histology.Interfaces;
using Histo.QualityControl.Interfaces;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Auth;
using Histo.Web.Pages;
using Histo.Web.Pages.Batches;
using Histo.Web.Pages.Submissions;
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

/// <summary>Client-supplied batch/child IDs on Customer-reachable pages must be ownership-checked.</summary>
public class IdOwnershipGuardTests
{
    private const int OwnArea = 4;
    private const int OtherArea = 3;
    private const int BatchId = 777;

    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ISubmissionService> _submissions = new();

    public IdOwnershipGuardTests()
    {
        _session.Setup(s => s.UserAreaID).Returns(OwnArea);
        _session.Setup(s => s.BatchID).Returns(BatchId);
    }

    private PageContext PageContextFor(string? query = null, string? form = null)
    {
        _batches.Setup(b => b.GetByIdAsync(BatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Batch { ID = BatchId, UserAreaCode = OtherArea });

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(AppClaimTypes.GroupName, "Customer")], "test")),
            RequestServices = new ServiceCollection().AddSingleton(_batches.Object).BuildServiceProvider(),
        };
        if (query is not null) httpContext.Request.QueryString = new QueryString(query);
        if (form is not null)
        {
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            var parts = form.Split('=');
            httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { [parts[0]] = parts[1] });
        }

        return new PageContext(new ActionContext(httpContext, new RouteData(), new PageActionDescriptor()));
    }

    private static async Task<(IActionResult? Result, bool NextCalled)> RunAsync(HistoPageModel model, PageContext pageContext)
    {
        model.PageContext = pageContext;
        var context = new PageHandlerExecutingContext(
            pageContext, [], new HandlerMethodDescriptor(), new Dictionary<string, object?>(), model);
        var nextCalled = false;

        await model.OnPageHandlerExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new PageHandlerExecutedContext(pageContext, [], new HandlerMethodDescriptor(), model));
        });

        return (context.Result, nextCalled);
    }

    private static void AssertForbidden((IActionResult? Result, bool NextCalled) outcome)
    {
        Assert.False(outcome.NextCalled);
        Assert.IsType<ForbidResult>(outcome.Result);
    }

    [Fact]
    public async Task SampleSummary_OtherAreaBatchInSession_IsForbidden()
    {
        var model = new SampleSummaryModel(_session.Object, _submissions.Object, _batches.Object, Mock.Of<IBlockService>(), Mock.Of<ILookupService>());

        AssertForbidden(await RunAsync(model, PageContextFor()));
    }

    [Fact]
    public async Task ViewSubmissions_SelectedOtherAreaBatch_IsForbidden()
    {
        var model = new ViewSubmissionsModel(_session.Object, _batches.Object, Mock.Of<IUserService>(), Mock.Of<ILookupService>(),
            Mock.Of<IBlockService>(), _submissions.Object, Mock.Of<IBlockTestService>()) { SelectedBatchId = BatchId };

        AssertForbidden(await RunAsync(model, PageContextFor()));
    }

    [Fact]
    public async Task DateReturned_OtherAreaBatchInSession_IsForbidden()
    {
        var model = new DateReturnedModel(_session.Object, _batches.Object, Mock.Of<ILookupService>());

        AssertForbidden(await RunAsync(model, PageContextFor()));
    }

    [Fact]
    public async Task PrintSubmission_OtherAreaBatchInSession_IsForbidden()
    {
        var model = new PrintSubmissionModel(_session.Object, _batches.Object, Mock.Of<IBlockService>(), _submissions.Object, Mock.Of<IBlockTestService>());

        AssertForbidden(await RunAsync(model, PageContextFor()));
    }

    [Fact]
    public async Task CopyBatch_SourceBatchInQuery_IsForbidden()
    {
        var model = new CopyBatchModel(_session.Object, _batches.Object, _submissions.Object, Mock.Of<ILookupService>());

        AssertForbidden(await RunAsync(model, PageContextFor(query: $"?sourceBatchId={BatchId}")));
    }

    [Fact]
    public async Task CopyBatch_SourceBatchInForm_IsForbidden()
    {
        var model = new CopyBatchModel(_session.Object, _batches.Object, _submissions.Object, Mock.Of<ILookupService>());

        AssertForbidden(await RunAsync(model, PageContextFor(form: $"SourceBatchId={BatchId}")));
    }

    [Fact]
    public async Task CopyBatchSummary_NewBatchInForm_IsForbidden()
    {
        _session.Setup(s => s.BatchID).Returns((int?)null);
        var model = new CopyBatchSummaryModel(_session.Object, _batches.Object, _submissions.Object);

        AssertForbidden(await RunAsync(model, PageContextFor(form: $"newBatchId={BatchId}")));
    }

    [Fact]
    public void RequestValues_IgnoresNonPositiveAndNonNumericValues()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?a=0&b=abc&c=5");

        Assert.Null(RequestValues.GetPositiveInt(context.Request, "a", "b"));
        Assert.Equal(5, RequestValues.GetPositiveInt(context.Request, "a", "c"));
    }

    [Fact]
    public async Task SubmissionDetails_DeleteTissueNotOnThisSample_IsNotDeleted()
    {
        _session.Setup(s => s.AnimalID).Returns(2);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(BatchId, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(BatchId, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2 }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(BatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 50, BatchID = BatchId, AnimalID = 2 }]);
        _submissions.Setup(s => s.GetTissuesBySubmissionAsync(BatchId, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[new Tissue { ID = 10, OwnerID = 50 }]);
        var model = new SubmissionDetailsModel(_session.Object, _submissions.Object, _batches.Object, Mock.Of<ILookupService>())
        {
            BatchId = BatchId,
        };

        await model.OnPostDeleteTissueAsync(999);
        await model.OnPostDeleteTissueAsync(10);

        _submissions.Verify(s => s.DeleteTissueAsync(999, It.IsAny<TissueOwner>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _submissions.Verify(s => s.DeleteTissueAsync(10, TissueOwner.Submission, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
