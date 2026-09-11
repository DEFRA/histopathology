using Microsoft.Azure.WebJobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Histo.WebJobs;

/// <summary>
/// Triggered WebJob replacing the legacy SQL Server Agent job "Histology Reset Histology Numbers"
/// (EXECUTE EditResetHistologyRef annually on 1 January at 04:00 UTC).
/// </summary>
public sealed class HistologyResetJob
{
    private readonly IConfiguration _config;

    public HistologyResetJob(IConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    // [FunctionName] + [TimerTrigger] are required by the WebJobs SDK JobHost to discover and schedule this method.
    [FunctionName("ResetHistologyNumbers")]
    public async Task ResetHistologyNumbersAsync(
        [TimerTrigger("0 0 4 1 1 *")] TimerInfo timer,
        ILogger log)
    {
        try
        {
            log.LogInformation(
                "ResetHistologyNumbers WebJob triggered at {UtcNow}. " +
                "Next execution scheduled: {ScheduleStatus}",
                DateTimeOffset.UtcNow,
                timer?.Schedule?.ToString() ?? "unknown");

            var connectionString = _config.GetConnectionString("HistologyDb")
                ?? throw new InvalidOperationException(
                    "Connection string 'HistologyDb' is not configured in appsettings.json.");

            await using var connection = new SqlConnection(connectionString);

            log.LogInformation("Opening SQL connection to Histology database...");
            await connection.OpenAsync();

            log.LogInformation("Connection opened successfully. Executing EditResetHistologyRef...");

            await using var command = new SqlCommand("EXECUTE dbo.EditResetHistologyRef", connection)
            {
                CommandTimeout = 60  // 60-second timeout for the stored procedure
            };

            await command.ExecuteNonQueryAsync();

            log.LogInformation(
                "EditResetHistologyRef completed successfully at {UtcNow}",
                DateTimeOffset.UtcNow);
        }
        catch (SqlException sqlEx)
        {
            log.LogError(
                sqlEx,
                "SQL error while executing EditResetHistologyRef. " +
                "Error number: {ErrorNumber}, Severity: {Severity}",
                sqlEx.Number,
                sqlEx.ClientConnectionId);

            throw;
        }
        catch (InvalidOperationException invOpEx)
        {
            log.LogError(
                invOpEx,
                "Configuration error: {Message}",
                invOpEx.Message);

            throw;
        }
        catch (Exception ex)
        {
            log.LogError(
                ex,
                "Unexpected error while executing ResetHistologyNumbers WebJob");

            throw;
        }
    }
}
