using System.Net;
using System.Text.RegularExpressions;

namespace Histo.E2ETests;

/// <summary>
/// Cross-role and cross-area behaviour through the real pipeline and a real database copy.
/// Skipped unless HISTO_E2E_SERVER and HISTO_E2E_SOURCE_DB are set.
/// </summary>
[Collection(E2eCollection.Name)]
public partial class CrossAreaEndToEndTests(E2eHost host)
{
    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();

    private TestUser Customer => TestUser.Customer(host.CustomerArea, host.CustomerUserId);

    private static void RequireDatabase(E2eHost h) =>
        Assert.SkipUnless(E2eHost.Enabled, "Set HISTO_E2E_SERVER and HISTO_E2E_SOURCE_DB to run the database-backed end-to-end tests.");

    private static bool IsDenied(HttpResponseMessage r) =>
        r.StatusCode == HttpStatusCode.Forbidden
        || (r.Headers.Location?.OriginalString.StartsWith("/AccessDenied", StringComparison.OrdinalIgnoreCase) ?? false);

    private static string SampleSummary(int batchId) => $"/Submissions/SampleSummary?batchId={batchId}";

    private async Task<bool> ByPassSortAsync(int batchId) =>
        await host.Database.ScalarAsync<bool>("SELECT ByPassSort FROM Batch WHERE ID = @id", ("@id", batchId));

    private static async Task<string> TokenFromAsync(HttpClient client, string page)
    {
        var html = await (await client.GetAsync(page, TestContext.Current.CancellationToken)).Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var match = TokenPattern().Match(html);
        Assert.True(match.Success, "Expected the page to render a form with an antiforgery token.");
        return match.Groups[1].Value;
    }

    private static Task<HttpResponseMessage> PostToggleAsync(HttpClient client, int batchId, string token) =>
        client.PostAsync($"/Submissions/SampleSummary?handler=ToggleByPassSort&batchId={batchId}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Customer_SeesTheirOwnAreasBatch_AndIsDeniedAnotherAreas()
    {
        RequireDatabase(host);
        using var client = host.App.CreateClientAs(Customer);

        var own = await client.GetAsync(SampleSummary(host.OwnBatchId), TestContext.Current.CancellationToken);
        var other = await client.GetAsync(SampleSummary(host.OtherAreaBatchId), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.True(IsDenied(other));
    }

    [Fact]
    public async Task HistoAndMaintenanceUsers_SeeBatchesFromEveryArea()
    {
        RequireDatabase(host);

        foreach (var user in new[] { TestUser.HistoUser(host.CustomerUserId), TestUser.MaintenanceUser(host.CustomerUserId) })
        {
            using var client = host.App.CreateClientAs(user);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(SampleSummary(host.OtherAreaBatchId), TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(SampleSummary(host.OwnBatchId), TestContext.Current.CancellationToken)).StatusCode);
        }
    }

    [Fact]
    public async Task Customer_CannotChangeAnotherAreasBatch_AndTheDatabaseIsUntouched()
    {
        RequireDatabase(host);
        using var client = host.App.CreateClientAs(Customer);
        var token = await TokenFromAsync(client, SampleSummary(host.OwnBatchId));
        var before = await ByPassSortAsync(host.OtherAreaBatchId);

        var response = await PostToggleAsync(client, host.OtherAreaBatchId, token);

        Assert.True(IsDenied(response));
        Assert.Equal(before, await ByPassSortAsync(host.OtherAreaBatchId));
    }

    [Fact]
    public async Task Customer_CanChangeTheirOwnAreasBatch_AndItPersists()
    {
        RequireDatabase(host);
        using var client = host.App.CreateClientAs(Customer);
        var token = await TokenFromAsync(client, SampleSummary(host.OwnBatchId));
        var before = await ByPassSortAsync(host.OwnBatchId);

        var response = await PostToggleAsync(client, host.OwnBatchId, token);

        Assert.False(IsDenied(response));
        Assert.Equal(!before, await ByPassSortAsync(host.OwnBatchId));
    }

    [Fact]
    public async Task Customer_IsDeniedMaintenancePagesAndSentAwayFromHistoPages()
    {
        RequireDatabase(host);
        using var client = host.App.CreateClientAs(Customer);

        Assert.True(IsDenied(await client.GetAsync("/Admin/UserMaintenance", TestContext.Current.CancellationToken)));
        var search = await client.GetAsync("/Search/SearchMenu", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Found, search.StatusCode);
    }

    [Fact]
    public async Task MaintenanceUser_OpensTheUserAdminPageWithRealData()
    {
        RequireDatabase(host);
        using var client = host.App.CreateClientAs(TestUser.MaintenanceUser(host.CustomerUserId));

        var response = await client.GetAsync("/Admin/UserMaintenance", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
