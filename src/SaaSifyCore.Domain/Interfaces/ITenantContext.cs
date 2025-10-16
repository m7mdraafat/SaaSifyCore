namespace SaaSifyCore.Domain.Interfaces;

public interface ITenantContext
{
    Guid? TenantId { get; }
    string? TenantSubdomain { get; }
}