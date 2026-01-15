using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rsl.Core.Entities;

namespace Rsl.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for XPost.
/// </summary>
public class XPostConfiguration : IEntityTypeConfiguration<XPost>
{
    public void Configure(EntityTypeBuilder<XPost> builder)
    {
        builder.ToTable("XPosts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PostId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Text)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.Url)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.PostCreatedAt)
            .IsRequired();

        builder.Property(x => x.AuthorXUserId)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.AuthorHandle)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.AuthorName)
            .HasMaxLength(200);

        builder.Property(x => x.AuthorProfileImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.MediaJson)
            .HasMaxLength(8000);

        builder.Property(x => x.IngestedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.PostId })
            .IsUnique();

        builder.HasIndex(x => new { x.UserId, x.PostCreatedAt });

        builder.HasOne(x => x.User)
            .WithMany(u => u.XPosts)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
