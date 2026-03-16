using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Crs.Core.Entities;

namespace Crs.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for RefreshToken.
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(x => x.Token);

        builder.Property(x => x.Token)
            .HasMaxLength(256);

        builder.HasIndex(x => x.ExpiresAt);
    }
}
