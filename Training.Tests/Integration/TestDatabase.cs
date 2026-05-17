using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace Training.Tests.Integration;

public static class TestDatabase
{
    /// <summary>
    /// Using docker compose to create a test DB, as schema and seeds are already declared in there
    /// </summary>
    /// <param name="databaseName">Test DB name, in order to create a different DB in the same container</param>
    /// <returns></returns>
    public static async Task InitializeAsync(string databaseName)
    {
        // Find root folder
        string repoRoot = FindRepoRoot();

        // Prepare docker compose command to create a temp test DB
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("compose");
        startInfo.ArgumentList.Add("run");      // docker compose run. It doesn't exec the whole app, instead it exec temporanely db-init service defined in docker-compose.yml
        startInfo.ArgumentList.Add("--rm");     // When this service ends, remove it
        startInfo.ArgumentList.Add("-e");       // We are going to override the env variable DB_NAME
        startInfo.ArgumentList.Add($"DB_NAME={databaseName}");
        startInfo.ArgumentList.Add("db-init");  // the name of the service we want to exec

        // Execute docker compose command
        using var process = Process.Start(startInfo) ??
            throw new InvalidOperationException("Failed to start docker compose process");

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"""
                Docker compose failed with exit code {process.ExitCode}

                Working directory:
                {repoRoot}

                Command:
                {startInfo.FileName} {string.Join(" ", startInfo.ArgumentList)}

                Output:
                {output}

                Error:
                {error}
            """);
        }
    }

    public static async Task DropAsync(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);

        // Save DB name before connecting to master DB, needed to drop the test DB
        string databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        SqlConnection.ClearAllPools();

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        // ALTER DATABASE force kill all connections to the test DB, needed before drop it
        await using var command = new SqlCommand($"""
            IF DB_ID(N'{databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{databaseName}];
            END
            """, connection);

        await command.ExecuteNonQueryAsync();
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string composePath = Path.Combine(directory.FullName, "docker-compose.yml");

            if (File.Exists(composePath))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not find docker-compose.yml");
    }
}