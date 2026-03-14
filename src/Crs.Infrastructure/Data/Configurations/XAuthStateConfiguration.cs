using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Crs.Core.Entities;

namespace Crs.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for XAuthState.
/// </summary>
public class XAuthStateConfiguration : IEntityTypeConfiguration<XAuthState>
{
    public void Configure(EntityTypeBuilder<XAuthState> builder)
    {
        builder.ToTable("XAuthStates");

        builder.HasKey(x => x.State);

        builder.Property(x => x.State)
            .HasMaxLength(64);

        builder.Property(x => x.CodeVerifier)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.RedirectUri)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(x => x.ExpiresAt);
    }
}
