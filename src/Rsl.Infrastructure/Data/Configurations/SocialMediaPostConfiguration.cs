using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rsl.Core.Entities;

namespace Rsl.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the SocialMediaPost entity (inherits from Resource).
/// </summary>
public class SocialMediaPostConfiguration : IEntityTypeConfiguration<SocialMediaPost>
{
    public void Configure(EntityTypeBuilder<SocialMediaPost> builder)
    {
        // Platform: optional, max length 50
        builder.Property(s => s.Platform)
            .HasMaxLength(50);

        // Username: optional, max length 100
        builder.Property(s => s.Username)
            .HasMaxLength(100);
    }
}

