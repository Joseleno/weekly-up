using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeeklyUp.Domain.Entities;

namespace WeeklyUp.Infrastructure.Persistence.Configurations;

public sealed class IntegrationConfiguration : IEntityTypeConfiguration<Integration>
{
    public void Configure(EntityTypeBuilder<Integration> builder)
    {
        builder.ToTable("integrations");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.CreatedAt).HasColumnName("created_at");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");

        builder.Property(i => i.UserId)
            .HasColumnName("user_id");

        builder.Property(i => i.Provider)
            .HasColumnName("provider")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.AccessToken)
            .HasColumnName("access_token")
            .IsRequired();

        builder.Property(i => i.RefreshToken)
            .HasColumnName("refresh_token")
            .IsRequired();

        builder.Property(i => i.ProviderAccountId)
            .HasColumnName("provider_account_id")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.PropertyId)
            .HasColumnName("property_id")
            .HasMaxLength(200);

        builder.Property(i => i.LastSyncAt)
            .HasColumnName("last_sync_at");

        builder.Property(i => i.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(500);

        builder.HasIndex(i => new { i.UserId, i.Provider })
            .HasDatabaseName("ix_integrations_user_provider")
            .IsUnique()
            .HasFilter("status != 'Disconnected'");
    }
}
