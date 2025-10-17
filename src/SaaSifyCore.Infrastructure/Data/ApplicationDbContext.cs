using Microsoft.EntityFrameworkCore;
using SaaSifyCore.Domain.Common;
using SaaSifyCore.Domain.Entities;
using SaaSifyCore.Domain.Interfaces;

namespace SaaSifyCore.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantContext? _tenantContext;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext? tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Constructor for EF migrations (no ITenantContext needed)
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        _tenantContext = null;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // If multi-tenancy is enabled, apply a global query filter for tenant isolation (Row-level isolation)
        ApplyGlobalFilters(modelBuilder);

        // Seed initial data
        SeedData(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // BEST PRACTICE: Automatically set CreatedAt/UpdatedAt timestamps
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            // Set UpdatedAt using reflection (since BaseEntity uses protected setter)
            var property = entry.Entity.GetType().GetProperty("UpdatedAt");
            if (property != null)
            {
                property.SetValue(entry.Entity, DateTime.UtcNow);
            }
        }

        // BEST PRACTICE: Collect domain events before saving changes
        var domainEvents = ChangeTracker.Entries<BaseEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Save changes to the database
        var result = await base.SaveChangesAsync(cancellationToken);

        // BEST PRACTICE: Clear domain events after successful save
        // (In production, you'd dispatch these to handlers here via MediatR or similar)

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            entry.Entity.ClearDomainEvents();
        }

        // TODO: Dispatch domain events to handlers ( Application Layer responsibility)

        return result;
    }
    /// <summary>
    /// Applies global query filters for automatic tenant isolation.
    /// These filters ensure queries automatically filter by the current tenant.
    /// BEST PRACTICE: Defense-in-depth strategy - even if developer forgets WHERE clause, data is still isolated.
    /// </summary>
    private void ApplyGlobalFilters(ModelBuilder modelBuilder)
    {
        // Apply tenant filter to tenant-specific entities
        // Filter is bypassed when _tenantContext is null (migrations, seeding, admin operations)
        modelBuilder.Entity<User>().HasQueryFilter(u =>
            _tenantContext == null ||
            _tenantContext.TenantId == null ||
            u.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<Subscription>().HasQueryFilter(s =>
            _tenantContext == null ||
            _tenantContext.TenantId == null ||
            s.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<RefreshToken>().HasQueryFilter(rt => 
            _tenantContext == null || 
            _tenantContext.TenantId == null || 
            rt.User.TenantId == _tenantContext.TenantId);
        // NOTE: Tenant and SubscriptionPlan are NOT filtered (accessible to all)
    }

    /// <summary>
    /// Seeds initial data (SubscriptionPlans)
    /// BEST PRACTICE: Keep seed data in code for consistency across environments.
    /// </summary>
    /// <param name="modelBuilder"></param>
    private void SeedData(ModelBuilder modelBuilder)
    {
        var freePlanId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var proPlanId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var enterprisePlanId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        modelBuilder.Entity<SubscriptionPlan>().HasData(
            new
            {
                Id = freePlanId,
                Name = "Free",
                Description = "Perfect for getting started",
                PricePerMonth = 0m,
                MaxUsers = 500,
                MaxStorageGB = 1,
                IsActive = true,
                StripePriceId = (string?)null,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = (DateTime?)null
            },
            new
            {
                Id = proPlanId,
                Name = "Pro",
                Description = "For growing teams",
                PricePerMonth = 29.99m,
                MaxUsers = 1000,
                MaxStorageGB = 10,
                IsActive = true,
                StripePriceId = (string?)null,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = (DateTime?)null
            },
            new
            {
                Id = enterprisePlanId,
                Name = "Enterprise",
                Description = "For large organizations",
                PricePerMonth = 99.99m,
                MaxUsers = 3000,
                MaxStorageGB = 50,
                IsActive = true,
                StripePriceId = (string?)null,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = (DateTime?)null
            }
        );
    }
}