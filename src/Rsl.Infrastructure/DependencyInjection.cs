using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rsl.Core.Interfaces;
using Rsl.Infrastructure.Data;
using Rsl.Infrastructure.Repositories;

namespace Rsl.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure services with dependency injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure layer services (DbContext, repositories) into the DI container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register DbContext with SQL Server
        services.AddDbContext<RslDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure()
            )
        );

        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<ITopicRepository, TopicRepository>();
        services.AddScoped<IResourceVoteRepository, ResourceVoteRepository>();
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();

        return services;
    }
}

