namespace SaaSifyCore.Infrastructure.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaaSifyCore.Domain.Entities;
using SaaSifyCore.Domain.ValueObjects;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.Id)
            .ValueGeneratedNever();
        
        // Map TenantName value object - extract Value property
        builder.Property(t => t.Name)
            .HasConversion(
                name => name.Value,
                value => TenantName.Create(value))
            .HasColumnName("Name")
            .HasMaxLength(100)
            .IsRequired();
        
        // Map Subdomain value object - extract Value property
        builder.Property(t => t.Subdomain)
            .HasConversion(
                subdomain => subdomain.Value,
                value => SubDomain.Create(value))
            .HasColumnName("Subdomain")
            .HasMaxLength(63)
            .IsRequired();
        
        // Now we can create index on Subdomain
        builder.HasIndex(t => t.Subdomain)
            .IsUnique()
            .HasDatabaseName("IX_Tenants_Subdomain");
        
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        
        builder.Property(t => t.CreatedAt)
            .IsRequired();
        
        builder.Property(t => t.UpdatedAt)
            .IsRequired(false);
        
        builder.Property(t => t.SubscriptionExpiresAt)
            .IsRequired(false);
        
        builder.HasMany(t => t.Users)
            .WithOne(u => u.Tenant)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(t => t.Subscription)
            .WithOne(s => s.Tenant)
            .HasForeignKey<Subscription>(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Ignore(t => t.DomainEvents);
    }
}