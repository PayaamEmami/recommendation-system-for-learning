using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Crs.Core.Entities;

namespace Crs.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the ContentVote entity using Fluent API.
/// </summary>
public class ContentVoteConfiguration : IEntityTypeConfiguration<ContentVote>
{
    public void Configure(EntityTypeBuilder<ContentVote> builder)
    {
        builder.ToTable("ContentVotes");

        builder.HasKey(v => v.Id);

        // VoteType: required (enum stored as int)
        builder.Property(v => v.VoteType)
            .IsRequired()
            .HasConversion<int>();

        // Timestamps
        builder.Property(v => v.CreatedAt)
            .IsRequired();

        builder.Property(v => v.UpdatedAt)
            .IsRequired(false);

        // Composite unique index: A user can only have one vote per content
        builder.HasIndex(v => new { v.UserId, v.ContentId })
            .IsUnique();

        // Relationships are configured in User and Content configurations
    }
}

