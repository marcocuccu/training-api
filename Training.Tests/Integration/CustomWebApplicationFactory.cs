using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using DotNetEnv;

namespace Training.Tests.Integration;

/// <summary>
/// Custom configuration for test. Not using appsettings.json or .env, but creating custom config instead
/// We use the same docker container SQL, same user sa, but we create a separate temp DB for tests only
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public string TestDbName { get; } = $"TrainingIntegrationTests_{Guid.NewGuid():N}";   // NewGuid ensures unique name, :N removes dashes in it
    public string ConnectionString;

    public CustomWebApplicationFactory()
    {
        Env.TraversePath().Load();  // Package DotNetEnv, reads .env

        string? password = Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD");

        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("MSSQL_SA_PASSWORD not found");

        ConnectionString =
            $"Server=localhost,1433;" +
            $"Database={TestDbName};" +
            $"User Id=sa;" +
            $"Password={password};" +
            $"Encrypt=True;" +
            $"TrustServerCertificate=True";

        // Overriding env variables for testing here instead of ConfigureWebHost(), 
        // as that would be reached too late, jwt variables would be already used before overriding them
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        Environment.SetEnvironmentVariable("AppSettings__PasswordPepper", "TEST_PASSWORD_PEPPER_1234567890");
        Environment.SetEnvironmentVariable("Jwt__Key", "THIS_IS_A_LONG_TEST_SECRET_KEY_for_JWT_1234567890_1234567890_1234567890_1234567890");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "Training.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "Training.Tests.Client");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiresMin", "15");
        Environment.SetEnvironmentVariable("Jwt__RefreshTokenExpiresMin", "10080");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}