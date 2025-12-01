using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rsl.Core.Entities;
using Rsl.Core.Enums;

namespace Rsl.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the abstract Resource entity and its inheritance hierarchy.
/// Uses Table-Per-Hierarchy (TPH) strategy with a discriminator column.
/// </summary>
public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("Resources");

        builder.HasKey(r => r.Id);

        // Set up inheritance discriminator based on ResourceType enum
        builder.HasDiscriminator<ResourceType>("Type")
            .HasValue<Paper>(ResourceType.Paper)
            .HasValue<Video>(ResourceType.Video)
            .HasValue<BlogPost>(ResourceType.BlogPost)
            .HasValue<CurrentEvent>(ResourceType.CurrentEvent)
            .HasValue<SocialMediaPost>(ResourceType.SocialMediaPost);

        // Title: required, max length 500
        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(500);

        // URL: required, max length 2000
        builder.Property(r => r.Url)
            .IsRequired()
            .HasMaxLength(2000);

        // Description: optional, max length 5000
        builder.Property(r => r.Description)
            .HasMaxLength(5000);

        // Timestamps
        builder.Property(r => r.PublishedDate)
            .IsRequired(false);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .IsRequired();

        // Many-to-One relationship with Source (optional)
        builder.HasOne(r => r.Source)
            .WithMany(s => s.Resources)
            .HasForeignKey(r => r.SourceId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // One-to-Many relationship with ResourceVotes
        builder.HasMany(r => r.Votes)
            .WithOne(v => v.Resource)
            .HasForeignKey(v => v.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-Many relationship with Recommendations
        builder.HasMany(r => r.Recommendations)
            .WithOne(rec => rec.Resource)
            .HasForeignKey(rec => rec.ResourceId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete recommendations
    }
}

