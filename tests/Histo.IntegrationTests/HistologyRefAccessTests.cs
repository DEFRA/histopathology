using System.Net;

namespace Histo.IntegrationTests;

/// <summary>Role and batch-ownership access to every page that creates, books, edits or lists histology refs, through the real auth pipeline.</summary>
[Collection(HostCollection.Name)]
public class HistologyRefAccessTests(HistoWebFactory factory)
{
    private static bool IsDenied(HttpResponseMessage r) =>
        r.StatusCode == HttpStatusCode.Forbidden
        || (r.Headers.Location?.OriginalString.StartsWith("/AccessDenied", StringComparison.OrdinalIgnoreCase) ?? false);

    private static bool IsSentHome(HttpResponseMessage r)
    {
        var location = r.Headers.Location?.OriginalString;
        return location == "/" || (location?.StartsWith("/Index", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private async Task<HttpResponseMessage> GetAsync(TestUser user, string path)
    {
        using var client = factory.CreateClientAs(user);
        return await client.GetAsync(path, TestContext.Current.CancellationToken);
    }

    public static TheoryData<string> HistoOrMaintenanceHrPages => new()
    {
        "/Bookings/BookingMenu",
        "/Bookings/BookHistologyRef",
        "/Search/SearchUnUsedHistologyRefs",
        "/Blocks/CopyBlocks",
    };

    public static TheoryData<string> MaintenanceOnlyHrPages => new()
    {
        "/Bookings/EditHistologyRef",
        "/Admin/EditAnimalRef",
    };

    [Theory]
    [MemberData(nameof(HistoOrMaintenanceHrPages))]
    public async Task Customer_IsSentHomeFromHistologyRefPages(string path)
    {
        Assert.True(IsSentHome(await GetAsync(TestUser.Customer(5), path)));
    }

    [Theory]
    [MemberData(nameof(MaintenanceOnlyHrPages))]
    public async Task Customer_IsDeniedTheCounterAndRefEditingPages(string path)
    {
        Assert.True(IsDenied(await GetAsync(TestUser.Customer(5), path)));
    }

    [Theory]
    [MemberData(nameof(MaintenanceOnlyHrPages))]
    public async Task HistoUser_IsDeniedTheCounterAndRefEditingPages(string path)
    {
        Assert.True(IsDenied(await GetAsync(TestUser.Histo, path)));
    }

    [Theory]
    [MemberData(nameof(HistoOrMaintenanceHrPages))]
    public async Task HistoUser_CanOpenTheBookingAndUnusedRefPages(string path)
    {
        var response = await GetAsync(TestUser.Histo, path);

        Assert.False(IsDenied(response));
        Assert.False(IsSentHome(response) && path != "/Blocks/CopyBlocks");
    }

    [Theory]
    [MemberData(nameof(HistoOrMaintenanceHrPages))]
    [MemberData(nameof(MaintenanceOnlyHrPages))]
    public async Task Maintenance_CanOpenEveryHistologyRefPage(string path)
    {
        var response = await GetAsync(TestUser.Maintenance, path);

        Assert.False(IsDenied(response));
        Assert.False(IsSentHome(response) && path != "/Blocks/CopyBlocks");
    }

    [Theory]
    [InlineData("/Submissions/SubmissionDetailsBlock?batchId=300&animalId=1")]
    [InlineData("/Submissions/AddSubmission?batchId=300&sourceAnimalId=1")]
    [InlineData("/Batches/CopyBatch?sourceBatchId=300")]
    [InlineData("/Batches/CopyBatchBlocksSummary?sourceBatchId=300")]
    [InlineData("/Batches/CopyBatchBlocksSummary/300")]
    [InlineData("/Batches/CopyBatchSummary?newBatchId=300")]
    public async Task Customer_IsDeniedHistologyRefPagesForAnotherAreasBatch(string path)
    {
        factory.AddBatch(300, areaCode: 7);

        Assert.True(IsDenied(await GetAsync(TestUser.Customer(5), path)));
    }

    [Theory]
    [InlineData("/Submissions/SubmissionDetailsBlock?batchId=300&animalId=1")]
    [InlineData("/Batches/CopyBatch?sourceBatchId=300")]
    public async Task HistoUser_IsNotDeniedAnotherAreasBatch(string path)
    {
        factory.AddBatch(300, areaCode: 7);

        Assert.False(IsDenied(await GetAsync(TestUser.Histo, path)));
    }
}
