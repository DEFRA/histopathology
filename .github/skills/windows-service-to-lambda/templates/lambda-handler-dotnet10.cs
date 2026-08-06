// ─── Lambda Handler Template — .NET 10 (dotnet10 runtime) ────────────────────
// MIGRATED FROM: Windows Service <ServiceName> (<OriginalProjectFile>)
// Original trigger: <OriginalTriggerDescription>
// Migrated trigger: EventBridge Scheduler <ScheduleExpression>
// Source framework: .NET Framework <SourceFramework>
// Migration date: <YYYY-MM-DD>
//
// INSTRUCTIONS FOR AI AGENT:
// 1. Replace all <Placeholder> values with values from migration-plan.json.
// 2. Copy all private/internal methods from the original ServiceBase subclass into
//    the "MIGRATED BUSINESS LOGIC" section below.
// 3. Apply pattern replacements as documented in references/pattern-mapping.md.
// 4. Add a "// MIGRATED FROM: <original code>" comment to every translated line.
// 5. Add "// TODO: MANUAL REVIEW REQUIRED — <reason>" for any pattern that cannot
//    be safely translated automatically.
// 6. Never alter stored procedure names, parameter names, or SQL command text.
// ─────────────────────────────────────────────────────────────────────────────

using Amazon.Lambda.Core;
using Amazon.Lambda.CloudWatchEvents.ScheduledEvents;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Data;
using System.Data.SqlClient;

[assembly: LambdaSerializer(
    typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace <ServiceName>;

/// <summary>
/// Lambda handler for <ServiceName>.
/// Migrated from Windows Service: <OriginalServiceClass>
/// Original trigger: <OriginalTriggerDescription>
/// </summary>
public class Function
{
    // ─── Static singletons for reuse across warm invocations ─────────────────
    // MIGRATED FROM: Fields initialised in OnStart() or constructor

    private static readonly IAmazonSecretsManager SecretsManager =
        new AmazonSecretsManagerClient();

    // Lazy-singleton connection string — fetched once per warm container lifetime
    // MIGRATED FROM: ConfigurationManager.ConnectionStrings["<ConnectionStringKey>"]
    private static string? _connectionString;

    // ─── Constructor ──────────────────────────────────────────────────────────

    public Function() { }

    // Dependency-injection constructor for testing
    internal Function(IAmazonSecretsManager secretsManager)
    {
        // Replace static field for test isolation
        // (Use this overload in unit tests to inject mocks)
    }

    // ─── Lambda Entry Point ───────────────────────────────────────────────────
    // MIGRATED FROM: OnTimerElapsed / OnStart background thread
    // Trigger: EventBridge Scheduler <ScheduleExpression>

    public async Task FunctionHandler(ScheduledEvent evnt, ILambdaContext context)
    {
        context.Logger.LogInformation(
            // MIGRATED FROM: EventLog.WriteEntry("Starting run", EventLogEntryType.Information)
            $"[<ServiceName>] Invoked at {evnt.Time:O} | RequestId: {context.AwsRequestId}");

        try
        {
            var connStr = await GetConnectionStringAsync(context);
            await RunServiceLogicAsync(connStr, context);

            context.Logger.LogInformation($"[<ServiceName>] Completed successfully.");
        }
        catch (Exception ex)
        {
            // MIGRATED FROM: EventLog.WriteEntry(ex.Message, EventLogEntryType.Error)
            context.Logger.LogError(
                $"[<ServiceName>] Unhandled exception: {ex.GetType().Name} — {ex.Message}");
            throw; // Re-throw so Lambda marks invocation as failed → routes to DLQ
        }
    }

    // ─── MIGRATED BUSINESS LOGIC ─────────────────────────────────────────────
    // All methods in this section are direct translations of the original
    // Windows Service logic. Stored procedure names, parameter names, and SQL
    // command text must not be changed.

    private async Task RunServiceLogicAsync(string connStr, ILambdaContext context)
    {
        // MIGRATED FROM: <OriginalMethodName> in <OriginalFile>.cs
        // TODO: Paste translated business logic here.
        // Replace SqlConnection(ConfigurationManager...) with SqlConnection(connStr).
        // Replace EventLog.WriteEntry with context.Logger.LogInformation / LogError.
        // All other code is preserved verbatim.
        await Task.CompletedTask;
    }

    // ─── INFRASTRUCTURE HELPERS ──────────────────────────────────────────────

    private static async Task<string> GetConnectionStringAsync(ILambdaContext context)
    {
        if (_connectionString is not null)
            return _connectionString; // Reuse across warm invocations

        // MIGRATED FROM: ConfigurationManager.ConnectionStrings["<ConnectionStringKey>"]
        var secretId = Environment.GetEnvironmentVariable("<CONNECTION_SECRET_ENV_VAR>")
            ?? throw new InvalidOperationException(
                "<CONNECTION_SECRET_ENV_VAR> environment variable is not set.");

        context.Logger.LogInformation($"Fetching connection string from Secrets Manager: {secretId}");

        var request = new GetSecretValueRequest { SecretId = secretId };
        var response = await SecretsManager.GetSecretValueAsync(request);
        _connectionString = response.SecretString;
        return _connectionString;
    }
}
