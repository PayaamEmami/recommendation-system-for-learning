using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rsl.Core.Interfaces;
using Rsl.Infrastructure.Configuration;
using Rsl.Infrastructure.Data;
using Rsl.Infrastructure.Repositories;
using Rsl.Infrastructure.Services;
using Rsl.Infrastructure.VectorStore;

namespace Rsl.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure services with dependency injection.
/// </summary>
public static class DependencyInjection
{
  /// <summary>
  /// Registers all Infrastructure layer services (DbContext, repositories, vector store, embeddings) into the DI container.
  /// </summary>
  /// <param name="services">The service collection to add services to.</param>
  /// <param name="configuration">The application configuration.</param>
  /// <returns>The service collection for chaining.</returns>
  public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
  {
    // Register DbContext with PostgreSQL
    services.AddDbContext<RslDbContext>(options =>
        options.UseNpgsql(
            configuration.GetConnectionString("DefaultConnection"),
            npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()
        )
    );

    // Register repositories
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<ISourceRepository, SourceRepository>();
    services.AddScoped<IResourceRepository, ResourceRepository>();
    services.AddScoped<IResourceVoteRepository, ResourceVoteRepository>();
    services.AddScoped<IRecommendationRepository, RecommendationRepository>();
    services.AddScoped<IXConnectionRepository, XConnectionRepository>();
    services.AddScoped<IXFollowedAccountRepository, XFollowedAccountRepository>();
    services.AddScoped<IXSelectedAccountRepository, XSelectedAccountRepository>();
    services.AddScoped<IXPostRepository, XPostRepository>();

    // Register configuration settings
    services.Configure<EmbeddingSettings>(configuration.GetSection(EmbeddingSettings.SectionName));
    services.Configure<OpenSearchSettings>(configuration.GetSection(OpenSearchSettings.SectionName));
    services.Configure<XApiSettings>(configuration.GetSection(XApiSettings.SectionName));

    // Register embedding service (OpenAI)
    services.AddHttpClient();
    services.AddSingleton<IEmbeddingService, OpenAIEmbeddingService>();
    services.AddHttpClient<IXApiClient, XApiClient>();

    // Register vector store (OpenSearch)
    services.AddSingleton<IVectorStore, OpenSearchVectorStore>();

    // Register content fetcher service (handles HTML and RSS/XML feeds)
    services.AddHttpClient<IContentFetcherService, ContentFetcherService>();

    return services;
  }
}
