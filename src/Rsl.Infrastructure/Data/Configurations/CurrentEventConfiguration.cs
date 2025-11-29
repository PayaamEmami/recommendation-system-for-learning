using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rsl.Core.Entities;

namespace Rsl.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the CurrentEvent entity (inherits from Resource).
/// </summary>
public class CurrentEventConfiguration : IEntityTypeConfiguration<CurrentEvent>
{
    public void Configure(EntityTypeBuilder<CurrentEvent> builder)
    {
        // NewsOutlet: optional, max length 200
        builder.Property(c => c.NewsOutlet)
            .HasMaxLength(200);

        // Author: optional, max length 200
        builder.Property(c => c.Author)
            .HasMaxLength(200);

        // Region: optional, max length 100
        builder.Property(c => c.Region)
            .HasMaxLength(100);
    }
}

