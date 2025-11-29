using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rsl.Core.Entities;

namespace Rsl.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the User entity using Fluent API.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        // Email: required, unique, max length 255
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        // DisplayName: optional, max length 100
        builder.Property(u => u.DisplayName)
            .HasMaxLength(100);

        // Timestamps
        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.LastLoginAt)
            .IsRequired(false);

        // Many-to-Many relationship with Topics (User interests)
        builder.HasMany(u => u.InterestedTopics)
            .WithMany(t => t.InterestedUsers)
            .UsingEntity(j => j.ToTable("UserTopics"));

        // One-to-Many relationship with ResourceVotes
        builder.HasMany(u => u.Votes)
            .WithOne(v => v.User)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-Many relationship with Recommendations
        builder.HasMany(u => u.Recommendations)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

