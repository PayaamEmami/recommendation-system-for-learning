using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rsl.Core.Entities;

namespace Rsl.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for XFollowedAccount.
/// </summary>
public class XFollowedAccountConfiguration : IEntityTypeConfiguration<XFollowedAccount>
{
    public void Configure(EntityTypeBuilder<XFollowedAccount> builder)
    {
        builder.ToTable("XFollowedAccounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.XUserId)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.Handle)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DisplayName)
            .HasMaxLength(200);

        builder.Property(x => x.ProfileImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.FollowedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.XUserId })
            .IsUnique();

        builder.HasOne(x => x.User)
            .WithMany(u => u.XFollowedAccounts)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.SelectedAccounts)
            .WithOne(s => s.FollowedAccount)
            .HasForeignKey(s => s.XFollowedAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
