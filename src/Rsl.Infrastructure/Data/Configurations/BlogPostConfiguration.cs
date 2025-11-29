using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rsl.Core.Entities;

namespace Rsl.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the BlogPost entity (inherits from Resource).
/// </summary>
public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        // Author: optional, max length 200
        builder.Property(b => b.Author)
            .HasMaxLength(200);

        // Blog: optional, max length 200
        builder.Property(b => b.Blog)
            .HasMaxLength(200);
    }
}

