using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rsl.Core.Entities;

namespace Rsl.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the ResourceVote entity using Fluent API.
/// </summary>
public class ResourceVoteConfiguration : IEntityTypeConfiguration<ResourceVote>
{
    public void Configure(EntityTypeBuilder<ResourceVote> builder)
    {
        builder.ToTable("ResourceVotes");

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

        // Composite unique index: A user can only have one vote per resource
        builder.HasIndex(v => new { v.UserId, v.ResourceId })
            .IsUnique();

        // Relationships are configured in User and Resource configurations
    }
}

