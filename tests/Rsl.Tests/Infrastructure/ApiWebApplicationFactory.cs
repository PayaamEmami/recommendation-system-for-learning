using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rsl.Core.Interfaces;
using Rsl.Infrastructure.Data;
using Rsl.Llm.Services;

namespace Rsl.Tests.Infrastructure;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = PostgresTestContainerFixture.ConnectionString,
                ["JwtSettings:SecretKey"] = "test-secret-key-for-integration-tests-only",
                ["JwtSettings:Issuer"] = "Rsl.Api.Tests",
                ["JwtSettings:Audience"] = "Rsl.Web.Tests",
                ["JwtSettings:ExpirationMinutes"] = "60",
                ["JwtSettings:RefreshTokenExpirationDays"] = "7",
                ["Registration:Enabled"] = "true",
                ["Registration:DisabledMessage"] = "Registrations disabled",
                ["Embedding:ApiKey"] = "test-key",
                ["Embedding:ModelName"] = "test",
                ["Embedding:Dimensions"] = "3",
                ["Embedding:MaxBatchSize"] = "100",
                ["OpenSearch:Endpoint"] = "http://localhost:9200",
                ["OpenSearch:IndexName"] = "rsl-resources-test",
                ["OpenSearch:EmbeddingDimensions"] = "3",
                ["OpenSearch:Region"] = "us-west-2",
                ["OpenAI:ApiKey"] = "test-key",
                ["OpenAI:Model"] = "gpt-5-nano",
                ["OpenAI:MaxTokens"] = "2048",
                ["OpenAI:Temperature"] = "0",
                ["X:ClientId"] = "test-client",
                ["X:ClientSecret"] = "test-secret",
                ["X:RedirectUri"] = "http://localhost/callback",
                ["X:Scopes"] = "users.read",
                ["X:BaseUrl"] = "https://api.x.com",
                ["X:AuthorizationUrl"] = "https://x.com/i/oauth2/authorize",
                ["X:TokenUrl"] = "https://api.x.com/2/oauth2/token"
            };

            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<RslDbContext>>();
            services.RemoveAll<RslDbContext>();
            services.AddDbContext<RslDbContext>(options =>
                options.UseNpgsql(PostgresTestContainerFixture.ConnectionString));

            services.RemoveAll<IEmbeddingService>();
            services.RemoveAll<IVectorStore>();
            services.RemoveAll<ILlmClient>();
            services.RemoveAll<IIngestionAgent>();
            services.RemoveAll<IXApiClient>();
            services.RemoveAll<IContentFetcherService>();

            services.AddSingleton<IEmbeddingService, FakeEmbeddingService>();
            services.AddSingleton<IVectorStore, InMemoryVectorStore>();
            services.AddSingleton<ILlmClient, FakeLlmClient>();
            services.AddSingleton<IIngestionAgent, FakeIngestionAgent>();
            services.AddSingleton<IXApiClient, FakeXApiClient>();
            services.AddSingleton<IContentFetcherService, FakeContentFetcherService>();

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RslDbContext>();
            db.Database.Migrate();
        });
    }
}
