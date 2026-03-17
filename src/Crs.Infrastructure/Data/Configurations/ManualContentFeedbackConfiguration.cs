using Crs.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crs.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for manual preference feedback entered by a user.
/// </summary>
public class ManualContentFeedbackConfiguration : IEntityTypeConfiguration<ManualContentFeedback>
{
    public void Configure(EntityTypeBuilder<ManualContentFeedback> builder)
    {
        builder.ToTable("ManualContentFeedback");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.Description)
            .HasMaxLength(5000);

        builder.Property(f => f.Url)
            .HasMaxLength(2000);

        builder.Property(f => f.ContentType)
            .HasConversion<string>()
            .IsRequired(false);

        builder.Property(f => f.VoteType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        builder.Property(f => f.UpdatedAt)
            .IsRequired();

        builder.HasIndex(f => new { f.UserId, f.CreatedAt });
    }
}
