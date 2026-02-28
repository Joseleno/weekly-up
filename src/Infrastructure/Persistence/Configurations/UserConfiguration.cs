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

        builder.Property(u => u.VerificationToken)
            .HasColumnName("verification_token")
            .HasMaxLength(64)
            .IsRequired(false);

        builder.Property(u => u.VerificationTokenExpiresAt)
            .HasColumnName("verification_token_expires_at")
            .IsRequired(false);

        builder.Property(u => u.StripeCustomerId)
            .HasColumnName("stripe_customer_id")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.HasIndex(u => u.ExternalAuthId)
            .HasDatabaseName("ix_users_external_auth_id")
            .IsUnique()
            .HasFilter("external_auth_id IS NOT NULL");

        builder.HasIndex(u => u.StripeCustomerId)
            .HasDatabaseName("ix_users_stripe_customer_id")
            .IsUnique()
            .HasFilter("stripe_customer_id IS NOT NULL");

        builder.Ignore(u => u.DomainEvents);

        builder.HasMany(u => u.Integrations)
            .WithOne()
            .HasForeignKey(i => i.UserId)
            .HasConstraintName("fk_integrations_users")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
