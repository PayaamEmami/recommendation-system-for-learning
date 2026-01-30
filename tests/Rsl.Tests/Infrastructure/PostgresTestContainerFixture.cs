using Testcontainers.PostgreSql;

namespace Rsl.Tests.Infrastructure;

public static class PostgresTestContainerFixture
{
    private static PostgreSqlContainer? _container;

    public static string ConnectionString =>
        _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Postgres test container is not started.");

    public static async Task StartAsync()
    {
        if (_container != null)
        {
            return;
        }

        _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("rsldb_test")
            .WithUsername("rsladmin")
            .WithPassword("YourStrong@Passw0rd")
            .Build();

        await _container.StartAsync();
    }

    public static async Task StopAsync()
    {
        if (_container == null)
        {
            return;
        }

        await _container.StopAsync();
        await _container.DisposeAsync();
        _container = null;
    }
}
