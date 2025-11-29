using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rsl.Core.Entities;

namespace Rsl.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the Paper entity (inherits from Resource).
/// </summary>
public class PaperConfiguration : IEntityTypeConfiguration<Paper>
{
    public void Configure(EntityTypeBuilder<Paper> builder)
    {
        // DOI: optional, max length 100
        builder.Property(p => p.DOI)
            .HasMaxLength(100);

        // Journal: optional, max length 300
        builder.Property(p => p.Journal)
            .HasMaxLength(300);

        // Authors: stored as JSON array
        builder.Property(p => p.Authors)
            .HasConversion(
                v => string.Join(",", v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            )
            .HasMaxLength(1000);

        // PublicationYear: optional
        builder.Property(p => p.PublicationYear)
            .IsRequired(false);
    }
}

