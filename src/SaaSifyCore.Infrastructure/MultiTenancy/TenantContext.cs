using SaaSifyCore.Domain.Interfaces;

namespace SaaSifyCore.Infrastructure.MultiTenancy;

public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public string? Subdomain { get; private set; }

    public bool IsResolved => TenantId.HasValue;

    public void Set(Guid? tenantId, string? subdomain)
    {
        TenantId = tenantId;
        Subdomain = subdomain;
    }
}
