namespace Rsl.Tests.Infrastructure;

[TestClass]
public sealed class TestAssemblyHooks
{
    [AssemblyInitialize]
    public static async Task AssemblyInitialize(TestContext context)
    {
        Environment.SetEnvironmentVariable("JwtSettings__SecretKey", "test-secret-key-for-integration-tests-only");
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "Rsl.Api.Tests");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "Rsl.Web.Tests");
        Environment.SetEnvironmentVariable("JwtSettings__ExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("JwtSettings__RefreshTokenExpirationDays", "7");
        Environment.SetEnvironmentVariable("Registration__Enabled", "true");
        Environment.SetEnvironmentVariable("Registration__DisabledMessage", "Registrations disabled");

        await PostgresTestContainerFixture.StartAsync();
    }

    [AssemblyCleanup]
    public static async Task AssemblyCleanup()
    {
        await PostgresTestContainerFixture.StopAsync();
    }
}
