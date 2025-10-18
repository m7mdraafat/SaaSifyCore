using Microsoft.EntityFrameworkCore;
using SaaSifyCore.Domain.Entities;

namespace SaaSifyCore.Domain.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
