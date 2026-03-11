using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Crs.Core.Entities;

namespace Crs.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for the Content Recommendation System.
/// Manages database connections and entity configurations.
/// </summary>
public class CrsDbContext : DbContext, IDataProtectionKeyContext
{
    public CrsDbContext(DbContextOptions<CrsDbContext> options) : base(options)
    {
    }

    // Entity DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Source> Sources { get; set; }
    public DbSet<Resource> Resources { get; set; }
    public DbSet<Paper> Papers { get; set; }
    public DbSet<Video> Videos { get; set; }
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<ResourceVote> ResourceVotes { get; set; }
    public DbSet<Recommendation> Recommendations { get; set; }
    public DbSet<XConnection> XConnections { get; set; }
    public DbSet<XFollowedAccount> XFollowedAccounts { get; set; }
    public DbSet<XSelectedAccount> XSelectedAccounts { get; set; }
    public DbSet<XPost> XPosts { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CrsDbContext).Assembly);
    }
}

