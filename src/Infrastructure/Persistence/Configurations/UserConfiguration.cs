using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;

namespace WeeklyUp.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");

        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();

            email.HasIndex(e => e.Value)
                .HasDatabaseName("ix_users_email")
                .IsUnique();
        });

        builder.OwnsOne(u => u.BusinessName, bn =>
        {
            bn.Property(b => b.Value)
                .HasColumnName("business_name")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Property(u => u.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.BusinessType)
            .HasColumnName("business_type")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.Plan)
            .HasColumnName("plan")
            .HasConversion<string>()
            .HasDefaultValue(PlanType.Free)
            .HasMaxLength(50);

        builder.Property(u => u.Timezone)
            .HasColumnName("timezone")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.ExternalAuthId)
            .HasColumnName("external_auth_id")
            .HasMaxLength(200);

        builder.Property(u => u.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(30);

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active");

        builder.Property(u => u.IsEmailVerified)
            .HasColumnName("is_email_verified");

        builder.HasIndex(u => u.ExternalAuthId)
            .HasDatabaseName("ix_users_external_auth_id")
            .IsUnique()
            .HasFilter("external_auth_id IS NOT NULL");

        builder.Ignore(u => u.DomainEvents);

        builder.HasMany<Integration>()
            .WithOne()
            .HasForeignKey(i => i.UserId);
    }
}
