using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using WeeklyUp.Domain.Entities;

namespace WeeklyUp.Infrastructure.Persistence.Configurations;

public sealed class ManualMetricConfiguration : IEntityTypeConfiguration<ManualMetric>
{
    public void Configure(EntityTypeBuilder<ManualMetric> builder)
    {
        builder.ToTable("manual_metrics");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");

        builder.Property(m => m.UserId)
            .HasColumnName("user_id");

        builder.Property(m => m.WeekStart)
            .HasColumnName("week_start");

        builder.Property(m => m.Revenue)
            .HasColumnName("revenue")
            .HasColumnType("numeric(18,2)");

        builder.Property(m => m.SalesCount)
            .HasColumnName("sales_count");

        builder.Property(m => m.NewCustomers)
            .HasColumnName("new_customers");

        builder.Property(m => m.Visits)
            .HasColumnName("visits");

        builder.HasIndex(m => new { m.UserId, m.WeekStart })
            .IsUnique()
            .HasDatabaseName("ix_manual_metrics_user_week");
    }
}
