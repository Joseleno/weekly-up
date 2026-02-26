using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.ValueObjects;

namespace WeeklyUp.Infrastructure.Persistence.Configurations;

public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        builder.Property(r => r.UserId)
            .HasColumnName("user_id");

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.EmailSentAt)
            .HasColumnName("email_sent_at");

        builder.Property(r => r.WhatsAppSentAt)
            .HasColumnName("whatsapp_sent_at");

        ConfigureWeekRange(builder);
        ConfigureMetrics(builder);
        ConfigureDemographics(builder);
        ConfigureInsights(builder);

        builder.HasIndex(r => r.UserId)
            .HasDatabaseName("ix_reports_user_created");

        builder.Ignore(r => r.DomainEvents);
    }

    private static void ConfigureWeekRange(EntityTypeBuilder<Report> builder)
    {
        builder.OwnsOne(r => r.WeekRange, wr =>
        {
            wr.Property(w => w.Start).HasColumnName("week_start");
            wr.Property(w => w.End).HasColumnName("week_end");
        });
    }

    private static void ConfigureMetrics(EntityTypeBuilder<Report> builder)
    {
        builder.OwnsOne(r => r.Metrics, m =>
        {
            m.ToJson("metrics");

            m.Property(x => x.SalesCount);
            m.Property(x => x.NewCustomers);
            m.Property(x => x.TotalVisits);
            m.Property(x => x.UniqueVisitors);
            m.Property(x => x.PageViews);
            m.Property(x => x.TopPage);
            m.Property(x => x.TopTrafficSource);
            m.Property(x => x.PreviousVisits);

            m.OwnsOne(x => x.Revenue, money =>
            {
                money.Property(mo => mo.Amount);
                money.Property(mo => mo.Currency);
            });

            m.OwnsOne(x => x.AverageTicket, money =>
            {
                money.Property(mo => mo.Amount);
                money.Property(mo => mo.Currency);
            });

            m.OwnsOne(x => x.PreviousRevenue, money =>
            {
                money.Property(mo => mo.Amount);
                money.Property(mo => mo.Currency);
            });
        });
    }

    private static void ConfigureDemographics(EntityTypeBuilder<Report> builder)
    {
        var converter = new ValueConverter<Demographics?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => v == null ? null : JsonSerializer.Deserialize<Demographics>(v, (JsonSerializerOptions?)null));

        builder.Property(r => r.Demographics)
            .HasColumnName("demographics")
            .HasColumnType("jsonb")
            .HasConversion(converter);
    }

    private static void ConfigureInsights(EntityTypeBuilder<Report> builder)
    {
        builder.OwnsOne(r => r.Insights, i => i.ToJson("insights"));
    }
}
