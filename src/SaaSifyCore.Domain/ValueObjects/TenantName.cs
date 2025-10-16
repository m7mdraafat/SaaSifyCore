using SaaSifyCore.Domain.Exceptions;

namespace SaaSifyCore.Domain.ValueObjects;
public record TenantName
{
    public string Value { get; }

    private TenantName(string value) => Value = value;

    public static TenantName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Tenant name cannot be empty");

        if (value.Length < 2 || value.Length > 100)
            throw new DomainException("Tenant name must be between 2 and 100 characters");

        return new TenantName(value.Trim());
    }

    public static implicit operator string(TenantName name) => name.Value;
}