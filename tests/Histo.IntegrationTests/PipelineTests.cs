using System.Net;
using System.Text.RegularExpressions;

namespace Histo.IntegrationTests;

/// <summary>Full request pipeline (auth, session, antiforgery, Razor rendering) with no database behind it.</summary>
[Collection(HostCollection.Name)]
public partial class PipelineTests(HistoWebFactory factory)
{
    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();

    [Fact]
    public async Task HomePage_RendersForACustomer()
    {
        using var client = factory.CreateClientAs(TestUser.Customer(5));

        var response = await client.GetAsync("/Index", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("govuk", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OwnAreaBatchPage_RendersForACustomer()
    {
        factory.AddBatch(200, areaCode: 5);
        using var client = factory.CreateClientAs(TestUser.Customer(5));

        var response = await client.GetAsync("/Submissions/SampleSummary?batchId=200", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OtherAreaBatchPage_IsDeniedBeforeAnyContentIsRendered()
    {
        factory.AddBatch(100, areaCode: 7);
        using var client = factory.CreateClientAs(TestUser.Customer(5));

        var response = await client.GetAsync("/Submissions/SampleSummary?batchId=100", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.StartsWith("/AccessDenied", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_WithoutAntiforgeryToken_IsRejected()
    {
        factory.AddBatch(200, areaCode: 5);
        using var client = factory.CreateClientAs(TestUser.Customer(5));

        var response = await client.PostAsync("/Submissions/SubmissionDetails?handler=AddTissue&batchId=200&animalId=1",
            new FormUrlEncodedContent([]), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ToAnotherAreasBatch_IsDeniedEvenWithAValidToken()
    {
        factory.AddBatch(200, areaCode: 5);
        factory.AddBatch(100, areaCode: 7);
        using var client = factory.CreateClientAs(TestUser.Customer(5));
        var token = await GetAntiforgeryTokenAsync(client, "/Submissions/SampleSummary?batchId=200");

        var response = await client.PostAsync("/Submissions/SubmissionDetails?handler=AddTissue&batchId=100&animalId=1",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.StartsWith("/AccessDenied", response.Headers.Location?.OriginalString);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string formPage)
    {
        var html = await (await client.GetAsync(formPage, TestContext.Current.CancellationToken)).Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var match = TokenPattern().Match(html);
        Assert.True(match.Success, "Expected the page to render a form with an antiforgery token.");
        return match.Groups[1].Value;
    }
}
