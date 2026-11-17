using System.Net;

namespace Histo.IntegrationTests;

/// <summary>The batch-ownership guard must apply whether the batch id is in the query string or the route.</summary>
[Collection(HostCollection.Name)]
public class BatchRouteAccessTests(HistoWebFactory factory)
{
    private static bool IsDenied(HttpResponseMessage r) =>
        r.StatusCode == HttpStatusCode.Forbidden
        || (r.Headers.Location?.OriginalString.StartsWith("/AccessDenied", StringComparison.OrdinalIgnoreCase) ?? false);

    [Theory]
    [InlineData("/Batches/CopyBatch?sourceBatchId=300")]
    [InlineData("/Batches/CopyBatch/300")]
    [InlineData("/Batches/CopyBatchSummary?newBatchId=300")]
    [InlineData("/Batches/CopyBatchSummary/300")]
    [InlineData("/Batches/CopyBatchBlocksSummary?sourceBatchId=300")]
    [InlineData("/Batches/CopyBatchBlocksSummary/300")]
    public async Task Customer_IsDeniedAnotherAreasBatch(string path)
    {
        factory.AddBatch(300, areaCode: 7);
        using var client = factory.CreateClientAs(TestUser.Customer(5));

        Assert.True(IsDenied(await client.GetAsync(path, TestContext.Current.CancellationToken)));
    }

    [Theory]
    [InlineData("/Batches/CopyBatch/300")]
    [InlineData("/Batches/CopyBatchSummary/300")]
    public async Task Histo_IsNotDeniedAnotherAreasBatch(string path)
    {
        factory.AddBatch(300, areaCode: 7);
        using var client = factory.CreateClientAs(TestUser.Histo);

        Assert.False(IsDenied(await client.GetAsync(path, TestContext.Current.CancellationToken)));
    }
}
