using System.Net;

namespace Histo.IntegrationTests;

[Collection(HostCollection.Name)]
public class AccessControlTests(HistoWebFactory factory)
{
    private static bool IsDenied(HttpResponseMessage r) =>
        r.StatusCode == HttpStatusCode.Forbidden
        || (r.Headers.Location?.OriginalString.StartsWith("/AccessDenied", StringComparison.OrdinalIgnoreCase) ?? false);

    // The Index page's route is "/", so RedirectToPage("/Index") yields Location "/".
    private static bool IsSentToHome(HttpResponseMessage r)
    {
        var location = r.Headers.Location?.OriginalString;
        return location == "/" || (location?.StartsWith("/Index", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private async Task<HttpResponseMessage> GetAsync(TestUser? user, string path)
    {
        using var client = factory.CreateClientAs(user);
        return await client.GetAsync(path);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/AccessibilityStatement")]
    [InlineData("/AccessDenied")]
    [InlineData("/SessionError")]
    [InlineData("/Errors/ServiceProblem")]
    public async Task PublicEndpoints_AreReachableAnonymously(string path)
    {
        var response = await GetAsync(null, path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/Index")]
    [InlineData("/Admin/UserMaintenance")]
    [InlineData("/Search/SearchMenu")]
    [InlineData("/Submissions/SubmissionDetails?batchId=100")]
    public async Task ProtectedPages_RedirectAnonymousUsersToSignIn(string path)
    {
        var response = await GetAsync(null, path);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/Saml2/login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task OldBatchBlockSummaryUrl_RedirectsAnonymousUsersStraightToSampleSummary()
    {
        var response = await GetAsync(null, "/Submissions/BatchBlockSummary?batchId=1");

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/Submissions/SampleSummary?batchId=1", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task SignedInUserWithNoUserRecord_IsSentToAccessDenied()
    {
        var response = await GetAsync(TestUser.UnknownUser, "/Index");

        Assert.True(IsDenied(response));
    }

    [Fact]
    public async Task MaintenanceOnlyPages_AreDeniedToCustomerAndHistoUsers()
    {
        Assert.True(IsDenied(await GetAsync(TestUser.Customer(5), "/Admin/UserMaintenance")));
        Assert.True(IsDenied(await GetAsync(TestUser.Histo, "/Admin/UserMaintenance")));
    }

    [Fact]
    public async Task MaintenanceOnlyPages_AreNotDeniedToMaintenanceUsers()
    {
        var response = await GetAsync(TestUser.Maintenance, "/Admin/UserMaintenance");

        Assert.False(IsDenied(response));
    }

    [Fact]
    public async Task HistoOrMaintenancePages_SendCustomersHome()
    {
        var response = await GetAsync(TestUser.Customer(5), "/Search/SearchMenu");

        Assert.True(IsSentToHome(response));
    }

    [Theory]
    [MemberData(nameof(HistoAndMaintenance))]
    public async Task HistoOrMaintenancePages_AreOpenToHistoAndMaintenanceUsers(string userName)
    {
        var user = UserNamed(userName);
        var response = await GetAsync(user, "/Search/SearchMenu");

        Assert.False(IsDenied(response));
        Assert.False(IsSentToHome(response));
    }

    [Theory]
    [InlineData("/Submissions/SubmissionDetails?batchId=100")]
    [InlineData("/Submissions/SampleSummary?batchId=100")]
    public async Task Customer_IsDeniedBatchesFromAnotherArea(string path)
    {
        factory.AddBatch(100, areaCode: 7);

        var response = await GetAsync(TestUser.Customer(5), path);

        Assert.True(IsDenied(response));
    }

    [Theory]
    [InlineData("/Submissions/SubmissionDetails?batchId=200")]
    [InlineData("/Submissions/SampleSummary?batchId=200")]
    public async Task Customer_IsNotDeniedBatchesFromTheirOwnArea(string path)
    {
        factory.AddBatch(200, areaCode: 5);

        var response = await GetAsync(TestUser.Customer(5), path);

        Assert.False(IsDenied(response));
    }

    [Theory]
    [MemberData(nameof(HistoAndMaintenance))]
    public async Task HistoAndMaintenanceUsers_AreNotDeniedBatchesFromAnyArea(string userName)
    {
        var user = UserNamed(userName);
        factory.AddBatch(100, areaCode: 7);

        var response = await GetAsync(user, "/Submissions/SubmissionDetails?batchId=100");

        Assert.False(IsDenied(response));
    }

    public static TheoryData<string> HistoAndMaintenance => new() { "Histo", "Maintenance" };

    private static TestUser UserNamed(string name) => name == "Histo" ? TestUser.Histo : TestUser.Maintenance;
}
