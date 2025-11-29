using Microsoft.EntityFrameworkCore;
using Rsl.Core.Entities;

namespace Rsl.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for the Recommendation System for Learning.
/// Manages database connections and entity configurations.
/// </summary>
public class RslDbContext : DbContext
{
    public RslDbContext(DbContextOptions<RslDbContext> options) : base(options)
    {
    }

    // Entity DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Resource> Resources { get; set; }
    public DbSet<Paper> Papers { get; set; }
    public DbSet<Video> Videos { get; set; }
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<CurrentEvent> CurrentEvents { get; set; }
    public DbSet<SocialMediaPost> SocialMediaPosts { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<ResourceVote> ResourceVotes { get; set; }
    public DbSet<Recommendation> Recommendations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RslDbContext).Assembly);
    }
}

