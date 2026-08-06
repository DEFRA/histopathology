# Playbook 05: Test Stub Generation

**Purpose**: Generate integration test stubs for every Lane 1 and Lane 2 Lambda handler.
Tests mock all AWS SDK clients, synthesise trigger payloads from the IaC schedule config,
and include a `baseline-capture` mode for recording Windows Service baseline outputs before
decommission.

**Input**: Generated handler files, `migration-output/migration-plan.json`  
**Output**: `migration-output/tests/<ServiceName>.Tests/`

---

## Step 1 — Determine Test Framework

Read `migration-plan.json → decisions.lambdaRuntime`:
- `dotnet10` → xUnit test project (`.csproj` + C# test files)

---

## Step 2 — xUnit Test Project

### 2.1 — Project File

`migration-output/tests/<ServiceName>.Tests/<ServiceName>.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework><HighestSdkTfm></TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.7.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.*">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.20.*" />
    <PackageReference Include="FluentAssertions" Version="6.12.*" />
    <PackageReference Include="Amazon.Lambda.TestUtilities" Version="1.4.*" />
    <PackageReference Include="AWSSDK.SecretsManager" Version="3.7.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\handlers\<ServiceName>\<ServiceName>.csproj" />
  </ItemGroup>
</Project>
```

### 2.2 — HandlerTests.cs

```csharp
using Amazon.Lambda.CloudWatchEvents.ScheduledEvents;
using Amazon.Lambda.TestUtilities;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using FluentAssertions;
using Moq;
using Xunit;

namespace <ServiceName>.Tests;

/// <summary>
/// Integration test stubs for Lambda handler migrated from Windows Service: <ServiceName>
/// All AWS SDK clients are mocked — no live AWS resources required.
/// </summary>
public class HandlerTests
{
    private readonly Mock<IAmazonSecretsManager> _mockSecrets;
    private readonly TestLambdaContext _context;

    public HandlerTests()
    {
        _mockSecrets = new Mock<IAmazonSecretsManager>();
        _context = new TestLambdaContext
        {
            FunctionName = "<lambdaFunctionName>",
            MemoryLimitInMB = <memorySizeMb>,
            RemainingTime = TimeSpan.FromSeconds(<timeoutSeconds>)
        };
    }

    // ─── Trigger Payload Tests ────────────────────────────────────────────────

    [Fact]
    public async Task FunctionHandler_WithValidScheduledEvent_CompletesSuccessfully()
    {
        // Arrange
        // TRIGGER PAYLOAD SYNTHESISED FROM: EventBridge schedule <scheduleExpression>
        // MIGRATED FROM: System.Timers.Timer.Elapsed event
        var scheduledEvent = new ScheduledEvent
        {
            // Synthesised from EventBridge Scheduler rule: <scheduleExpression>
            Account = "123456789012",
            Region = "<awsRegion>",
            Detail = new Dictionary<string, object>(),
            Time = DateTime.UtcNow,
            Id = Guid.NewGuid().ToString(),
            Resources = ["arn:aws:events:<awsRegion>:123456789012:rule/<lambdaFunctionName>-dev-schedule"],
            Source = "aws.events",
            DetailType = "Scheduled Event"
        };

        // Mock Secrets Manager — replaces ConfigurationManager.ConnectionStrings
        _mockSecrets
            .Setup(s => s.GetSecretValueAsync(
                It.Is<GetSecretValueRequest>(r => r.SecretId.Contains("MainDbConnection")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSecretValueResponse
            {
                SecretString = "Server=localhost;Database=TestDb;Integrated Security=true;"
            });

        var function = new Function(_mockSecrets.Object);

        // Act
        await function.FunctionHandler(scheduledEvent, _context);

        // Assert — handler must complete without exception
        // TODO: Add data-parity assertions using baseline fixtures once captured
    }

    // ─── Secrets Manager Tests ────────────────────────────────────────────────

    [Fact]
    public async Task FunctionHandler_WhenSecretsMissing_ThrowsAndLogsError()
    {
        // Arrange
        var scheduledEvent = BuildScheduledEvent();
        _mockSecrets
            .Setup(s => s.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ResourceNotFoundException("Secret not found"));

        var function = new Function(_mockSecrets.Object);

        // Act + Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => function.FunctionHandler(scheduledEvent, _context));
    }

    // ─── Data-Parity Tests (Baseline Capture) ─────────────────────────────────
    // These tests compare output of the migrated Lambda handler against baseline
    // fixtures recorded from the original Windows Service before decommission.

    [Fact(Skip = "Run manually after baseline fixtures captured from original Windows Service")]
    public async Task FunctionHandler_ProducesOutputMatchingWindowsServiceBaseline()
    {
        // Arrange
        var baselineFixture = await LoadBaselineFixtureAsync("baseline-output.json");
        var scheduledEvent = BuildScheduledEvent();

        // Mock Secrets with real test DB connection (provide via environment variable in CI)
        _mockSecrets
            .Setup(s => s.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSecretValueResponse
            {
                SecretString = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
                    ?? throw new InvalidOperationException("TEST_DB_CONNECTION not set")
            });

        var function = new Function(_mockSecrets.Object);

        // Act
        await function.FunctionHandler(scheduledEvent, _context);

        // Assert — compare actual DB state against baseline fixture
        // TODO: Load expected rows from baselineFixture and assert equivalence
        baselineFixture.Should().NotBeNull("baseline fixture must exist before parity test runs");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static ScheduledEvent BuildScheduledEvent() => new()
    {
        // SYNTHESISED FROM: EventBridge schedule expression <scheduleExpression>
        Account = "123456789012",
        Region = "<awsRegion>",
        Detail = new Dictionary<string, object>(),
        Time = DateTime.UtcNow,
        Id = Guid.NewGuid().ToString(),
        Source = "aws.events",
        DetailType = "Scheduled Event"
    };

    private static async Task<Dictionary<string, object>> LoadBaselineFixtureAsync(string fileName)
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory, "Fixtures", fileName);
        var json = await File.ReadAllTextAsync(fixturePath);
        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json)
            ?? new Dictionary<string, object>();
    }
}
```

### 2.3 — Baseline Trigger Payload Fixture

`migration-output/tests/<ServiceName>.Tests/Fixtures/baseline-trigger-payload.json`:

```json
{
  "_comment": "Synthesised EventBridge trigger payload for <ServiceName>. MIGRATED FROM: System.Timers.Timer <intervalMs>ms",
  "version": "0",
  "id": "test-event-id",
  "source": "aws.events",
  "account": "123456789012",
  "time": "2024-01-01T00:00:00Z",
  "region": "<awsRegion>",
  "resources": [
    "arn:aws:events:<awsRegion>:123456789012:rule/<lambdaFunctionName>-dev-schedule"
  ],
  "detail-type": "Scheduled Event",
  "detail": {}
}
```

---

## Step 3 — Test Validation

After generating all test projects:

**dotnet10**:
```bash
dotnet build migration-output/tests/<ServiceName>.Tests/<ServiceName>.Tests.csproj
dotnet test migration-output/tests/<ServiceName>.Tests/ --no-build --logger "console;verbosity=normal"
```

Expected: all non-skipped tests pass. Skipped (baseline) tests are expected to be skipped.

Flag any test failure as a **blocking issue** in the migration report with the full
failure message and a recommended resolution.
