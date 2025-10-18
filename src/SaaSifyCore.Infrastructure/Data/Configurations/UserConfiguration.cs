namespace SaaSifyCore.Infrastructure.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaaSifyCore.Domain.Entities;
using SaaSifyCore.Domain.ValueObjects;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Id)
            .ValueGeneratedNever();
        
        // Map Email value object using conversion
        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => Email.Create(value))
            .HasColumnName("Email")
            .HasMaxLength(256)
            .IsRequired();

        // Now we can create indexes normally
        builder.HasIndex(u => new { u.Email, u.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_Users_Email_TenantId")
            .IncludeProperties(u => new { u.PasswordHash, u.FirstName, u.LastName, u.Role }); // Covering index.

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        
        builder.Property(u => u.IsEmailVerified)
            .IsRequired();
        
        builder.Property(u => u.CreatedAt)
            .IsRequired();
        
        builder.Property(u => u.UpdatedAt)
            .IsRequired(false);
        
        builder.Property(u => u.TenantId)
            .IsRequired();
        
        builder.HasIndex(u => u.TenantId)
            .HasDatabaseName("IX_Users_TenantId");
        
        // Now composite index works
        builder.HasIndex(u => new { u.Email, u.TenantId })
            .HasDatabaseName("IX_Users_Email_TenantId");
        
        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Ignore(u => u.DomainEvents);
    }
}