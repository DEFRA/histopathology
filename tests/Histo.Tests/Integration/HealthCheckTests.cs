using System.Net;

namespace Histo.Tests.Integration;

/// <summary>
/// Integration smoke tests for the <c>Histo.Web</c> application.
///
/// These tests are skipped in Phase 0 because <c>Histo.Web</c> has not yet been
/// scaffolded. They will be enabled in Phase 1 once the Razor Pages project exists
/// and a health-check endpoint (<c>GET /health</c>) is registered.
///
/// To enable:
/// 1. Remove the <c>Skip</c> attribute.
/// 2. Set the environment variable or configuration key <c>HISTO_TEST_BASE_URL</c>
///    to the test application URL (e.g. https://localhost:5000).
/// 3. Update <c>.github/workflows/ci.yml</c> to start <c>Histo.Web</c> before
///    the integration test job runs.
/// </summary>
public class HealthCheckTests
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("HISTO_TEST_BASE_URL") ?? "https://localhost:5000";

    [Fact(Skip = "Phase 1 gate: scaffold Histo.Web and configure HISTO_TEST_BASE_URL before enabling.")]
    public async Task HealthEndpoint_WhenApplicationIsRunning_Returns200()
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri(BaseUrl);

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
