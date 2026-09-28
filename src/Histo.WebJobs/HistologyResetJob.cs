using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Histo.WebJobs;

/// <summary>
/// Triggered WebJob replacing the legacy SQL Server Agent job "Histology Reset Histology Numbers"
/// (EXECUTE EditResetHistologyRef annually on 1 January at 04:00 UTC).
///
/// This runs as a plain console app scheduled via <c>settings.job</c> (Kudu's built-in WebJob
/// scheduler), not the Azure WebJobs SDK's [TimerTrigger] — so it does not require an
/// AzureWebJobsStorage account. The process runs once, executes the stored procedure, and exits.
/// </summary>
public sealed class HistologyResetJob
{
    private readonly IConfiguration _config;
    private readonly ILogger _log;

    public HistologyResetJob(IConfiguration config, ILogger log)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <summary>Executes the reset. Returns <see langword="true"/> on success, <see langword="false"/> on failure.</summary>
    public async Task<bool> RunAsync()
    {
        try
        {
            _log.LogInformation("ResetHistologyNumbers WebJob started at {UtcNow}", DateTimeOffset.UtcNow);

            var connectionString = _config.GetConnectionString("HistologyDb")
                ?? throw new InvalidOperationException(
                    "Connection string 'HistologyDb' is not configured in appsettings.json.");

            await using var connection = new SqlConnection(connectionString);

            _log.LogInformation("Opening SQL connection to Histology database...");
            await connection.OpenAsync();

            _log.LogInformation("Connection opened successfully. Executing EditResetHistologyRef...");

            await using var command = new SqlCommand("EXECUTE dbo.EditResetHistologyRef", connection)
            {
                CommandTimeout = 60  // 60-second timeout for the stored procedure
            };

            await command.ExecuteNonQueryAsync();

            _log.LogInformation(
                "EditResetHistologyRef completed successfully at {UtcNow}",
                DateTimeOffset.UtcNow);

            return true;
        }
        catch (SqlException sqlEx)
        {
            _log.LogError(
                sqlEx,
                "SQL error while executing EditResetHistologyRef. " +
                "Error number: {ErrorNumber}, Severity: {Severity}",
                sqlEx.Number,
                sqlEx.ClientConnectionId);

            return false;
        }
        catch (InvalidOperationException invOpEx)
        {
            _log.LogError(
                invOpEx,
                "Configuration error: {Message}",
                invOpEx.Message);

            return false;
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Unexpected error while executing ResetHistologyNumbers WebJob");

            return false;
        }
    }
}
