using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaaSifyCore.Domain.Entities;

namespace SaaSifyCore.Infrastructure.Data.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Id)
            .ValueGeneratedNever(); // Domain generates GUIDs

        builder.Property(sp => sp.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sp => sp.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(sp => sp.PricePerMonth)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(sp => sp.MaxUsers)
            .IsRequired();

        builder.Property(sp => sp.MaxStorageGB)
            .IsRequired();

        builder.Property(sp => sp.IsActive)
            .IsRequired();

        builder.Property(sp => sp.StripePriceId)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(sp => sp.CreatedAt)
            .IsRequired();

        builder.Property(sp => sp.UpdatedAt)
            .IsRequired(false);

        builder.HasIndex(sp => sp.Name)
            .IsUnique()
            .HasDatabaseName("IX_SubscriptionPlans_Name");

        // Ignore domain events (not persisted)
        builder.Ignore(sp => sp.DomainEvents);
    }
}