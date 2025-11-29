using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rsl.Core.Entities;

namespace Rsl.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the Video entity (inherits from Resource).
/// </summary>
public class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        // Duration: optional, stored as ticks
        builder.Property(v => v.Duration)
            .IsRequired(false);

        // Channel: optional, max length 200
        builder.Property(v => v.Channel)
            .HasMaxLength(200);

        // ThumbnailUrl: optional, max length 2000
        builder.Property(v => v.ThumbnailUrl)
            .HasMaxLength(2000);
    }
}

