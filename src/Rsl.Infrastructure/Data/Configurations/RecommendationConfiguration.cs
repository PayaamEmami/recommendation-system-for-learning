using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rsl.Core.Entities;

namespace Rsl.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the Recommendation entity using Fluent API.
/// </summary>
public class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.ToTable("Recommendations");

        builder.HasKey(r => r.Id);

        // FeedType: required (enum stored as int)
        builder.Property(r => r.FeedType)
            .IsRequired()
            .HasConversion<int>();

        // Date: required
        builder.Property(r => r.Date)
            .IsRequired();

        // Position: required
        builder.Property(r => r.Position)
            .IsRequired();

        // Score: optional
        builder.Property(r => r.Score)
            .IsRequired(false);

        // GeneratedAt: required
        builder.Property(r => r.GeneratedAt)
            .IsRequired();

        // Composite index for efficient querying: User + Date + FeedType
        builder.HasIndex(r => new { r.UserId, r.Date, r.FeedType });

        // Index for querying by user and date
        builder.HasIndex(r => new { r.UserId, r.Date });

        // Relationships are configured in User and Resource configurations
    }
}

