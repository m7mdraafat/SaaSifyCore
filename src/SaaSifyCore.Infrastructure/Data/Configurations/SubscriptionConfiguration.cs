using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaaSifyCore.Domain.Entities;

namespace SaaSifyCore.Infrastructure.Data.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever(); // Domain generates GUIDs

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.StartDate)
            .IsRequired();

        builder.Property(s => s.EndDate)
            .IsRequired(false);

        builder.Property(s => s.CancelledAt)
            .IsRequired(false);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired(false);

        builder.Property(s => s.StripeSubscriptionId)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(s => s.StripeCustomerId)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(s => s.TenantId)
            .IsRequired();

        builder.HasIndex(s => s.TenantId)
            .IsUnique() // One subscription per tenant
            .HasDatabaseName("IX_Subscriptions_TenantId");

        builder.HasIndex(s => s.StripeSubscriptionId)
            .HasDatabaseName("IX_Subscriptions_StripeSubscriptionId");

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("IX_Subscriptions_Status");

        // Relationships
        builder.HasOne(s => s.Plan)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete plans when subscription is deleted

        // Ignore domain events (not persisted)
        builder.Ignore(s => s.DomainEvents);
    }
}