using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rsl.Core.Entities;

namespace Rsl.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the Source entity using Fluent API.
/// </summary>
public class SourceConfiguration : IEntityTypeConfiguration<Source>
{
    public void Configure(EntityTypeBuilder<Source> builder)
    {
        builder.ToTable("Sources");

        builder.HasKey(s => s.Id);

        // Name: required, max length 200
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Url: required, max length 2000
        builder.Property(s => s.Url)
            .IsRequired()
            .HasMaxLength(2000);

        // Description: optional, max length 1000
        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        // Category: required
        builder.Property(s => s.Category)
            .IsRequired();

        // IsActive: required, default true
        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Timestamps
        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // Index on UserId for faster queries
        builder.HasIndex(s => s.UserId);

        // Index on Category for faster filtering
        builder.HasIndex(s => s.Category);

        // Composite index for active sources by user
        builder.HasIndex(s => new { s.UserId, s.IsActive });
    }
}

