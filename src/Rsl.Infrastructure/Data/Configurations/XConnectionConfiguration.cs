using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rsl.Core.Entities;

namespace Rsl.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for XConnection.
/// </summary>
public class XConnectionConfiguration : IEntityTypeConfiguration<XConnection>
{
    public void Configure(EntityTypeBuilder<XConnection> builder)
    {
        builder.ToTable("XConnections");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.XUserId)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.Handle)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DisplayName)
            .HasMaxLength(200);

        builder.Property(x => x.AccessTokenEncrypted)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.RefreshTokenEncrypted)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.Scopes)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.TokenExpiresAt)
            .IsRequired(false);

        builder.Property(x => x.ConnectedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasIndex(x => x.XUserId);

        builder.HasOne(x => x.User)
            .WithMany(u => u.XConnections)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
