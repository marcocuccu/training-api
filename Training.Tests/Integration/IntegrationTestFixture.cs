namespace Training.Tests.Integration;

/// <summary>
/// Initializes and cleans contexts needed for tests
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime    // IAsyncLifetime is xUnit interface for async coding
{
    public CustomWebApplicationFactory Factory { get; } = new();

    public async Task InitializeAsync()
    {
        await TestDatabase.InitializeAsync(Factory.TestDbName);
    }

    public async Task DisposeAsync()
    {
        Factory.Dispose();
        await TestDatabase.DropAsync(Factory.ConnectionString);
    }
}