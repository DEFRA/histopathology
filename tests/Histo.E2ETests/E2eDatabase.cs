using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace Histo.E2ETests;

/// <summary>
/// A throwaway copy of an already-deployed Histology database, with this repo's src/Database/Deploy
/// script applied on top. The source database is only read (COPY_ONLY backup); tests write to the copy.
/// Opt in with HISTO_E2E_SERVER (e.g. "(localdb)\MSSQLLocalDB") and HISTO_E2E_SOURCE_DB (e.g. "Histology").
/// </summary>
public sealed class E2eDatabase : IAsyncDisposable
{
    private const string CopyName = "Histology_E2E";

    public static string? Server => Environment.GetEnvironmentVariable("HISTO_E2E_SERVER");
    public static string? SourceDb => Environment.GetEnvironmentVariable("HISTO_E2E_SOURCE_DB");
    public static bool Enabled => !string.IsNullOrWhiteSpace(Server) && !string.IsNullOrWhiteSpace(SourceDb);

    public string ConnectionString =>
        $"Server={Server};Database={CopyName};Integrated Security=True;TrustServerCertificate=True;Encrypt=False;";

    private static string MasterConnectionString =>
        $"Server={Server};Database=master;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;";

    public static async Task<E2eDatabase> CreateAsync()
    {
        var db = new E2eDatabase();
        await DropAsync();
        await RestoreFromSourceAsync();
        await db.DeployRepoScriptsAsync();
        return db;
    }

    public async Task<T?> ScalarAsync<T>(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? default : (T)Convert.ChangeType(result, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
    }

    public ValueTask DisposeAsync() => new(DropAsync());

    private static async Task ExecuteMasterAsync(string sql)
    {
        await using var connection = new SqlConnection(MasterConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 600 };
        await command.ExecuteNonQueryAsync();
    }

    private static Task DropAsync() => ExecuteMasterAsync($"""
        IF DB_ID('{CopyName}') IS NOT NULL
        BEGIN
            ALTER DATABASE [{CopyName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE [{CopyName}];
        END
        """);

    private static async Task RestoreFromSourceAsync()
    {
        var backupFile = Path.Combine(Path.GetTempPath(), $"{CopyName}.bak");
        try
        {
            await ExecuteMasterAsync($"BACKUP DATABASE [{SourceDb}] TO DISK = N'{backupFile}' WITH COPY_ONLY, INIT, FORMAT;");

            string moves;
            await using (var connection = new SqlConnection(MasterConnectionString))
            {
                await connection.OpenAsync();
                await using var command = new SqlCommand($"""
                    SELECT STRING_AGG(
                        'MOVE N''' + name + ''' TO N''' + CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(400))
                        + '{CopyName}_' + name + CASE type_desc WHEN 'LOG' THEN '.ldf' ELSE '.mdf' END + '''', ', ')
                    FROM sys.master_files WHERE database_id = DB_ID('{SourceDb}')
                    """, connection);
                moves = (string)(await command.ExecuteScalarAsync())!;
            }

            await ExecuteMasterAsync($"RESTORE DATABASE [{CopyName}] FROM DISK = N'{backupFile}' WITH REPLACE, {moves};");
        }
        finally
        {
            if (File.Exists(backupFile)) File.Delete(backupFile);
        }
    }

    // sqlcmd resolves the manifest's ":r" includes relative to the script's own folder.
    private async Task DeployRepoScriptsAsync()
    {
        var deployDir = FindDeployDirectory();
        var startInfo = new ProcessStartInfo("sqlcmd", $"-S \"{Server}\" -d {CopyName} -E -C -b -i Deploy.sql")
        {
            WorkingDirectory = deployDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(startInfo)!;
        var output = await process.StandardOutput.ReadToEndAsync() + await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Deploy.sql failed against the E2E copy (exit {process.ExitCode}):{Environment.NewLine}{output}");
    }

    private static string FindDeployDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Database", "Deploy");
            if (File.Exists(Path.Combine(candidate, "Deploy.sql"))) return candidate;
        }

        throw new DirectoryNotFoundException("Could not locate src/Database/Deploy/Deploy.sql above the test output folder.");
    }
}
