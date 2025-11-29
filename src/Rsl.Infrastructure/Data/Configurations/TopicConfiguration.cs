using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rsl.Core.Entities;

namespace Rsl.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the Topic entity using Fluent API.
/// </summary>
public class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.ToTable("Topics");

        builder.HasKey(t => t.Id);

        // Name: required, max length 200
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Description: optional, max length 1000
        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        // CreatedAt: required
        builder.Property(t => t.CreatedAt)
            .IsRequired();
    }
}

