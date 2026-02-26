using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeeklyUp.Domain.Entities;

namespace WeeklyUp.Infrastructure.Persistence.Configurations;

public sealed class ReportPreferenceConfiguration : IEntityTypeConfiguration<ReportPreference>
{
    public void Configure(EntityTypeBuilder<ReportPreference> builder)
    {
        builder.ToTable("report_preferences");

        builder.HasKey(rp => rp.Id);
        builder.Property(rp => rp.Id).HasColumnName("id");
        builder.Property(rp => rp.CreatedAt).HasColumnName("created_at");
        builder.Property(rp => rp.UpdatedAt).HasColumnName("updated_at");

        builder.Property(rp => rp.UserId)
            .HasColumnName("user_id");

        builder.Property(rp => rp.SendDay)
            .HasColumnName("send_day")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(rp => rp.SendTime)
            .HasColumnName("send_time");

        builder.Property(rp => rp.Language)
            .HasColumnName("language")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property<List<string>>("_enabledSections")
            .HasColumnName("enabled_sections")
            .HasColumnType("text[]");

        builder.HasIndex(rp => rp.UserId)
            .IsUnique()
            .HasDatabaseName("ix_report_preferences_user");
    }
}
