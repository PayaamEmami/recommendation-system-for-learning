using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rsl.Core.Entities;

namespace Rsl.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for XSelectedAccount.
/// </summary>
public class XSelectedAccountConfiguration : IEntityTypeConfiguration<XSelectedAccount>
{
    public void Configure(EntityTypeBuilder<XSelectedAccount> builder)
    {
        builder.ToTable("XSelectedAccounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SelectedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.XFollowedAccountId })
            .IsUnique();

        builder.HasOne(x => x.User)
            .WithMany(u => u.XSelectedAccounts)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.FollowedAccount)
            .WithMany(f => f.SelectedAccounts)
            .HasForeignKey(x => x.XFollowedAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Posts)
            .WithOne(p => p.SelectedAccount)
            .HasForeignKey(p => p.XSelectedAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
