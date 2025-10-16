using SaaSifyCore.Domain.Exceptions;
using System.Linq;
using System.Text.RegularExpressions;

namespace SaaSifyCore.Domain.ValueObjects;

public record SubDomain
{
    private static readonly Regex SubdomainRegex = new Regex(@"^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private SubDomain(string value) => Value = value;

    public static SubDomain Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Subdomain cannot be empty");

        var normalized = value.ToLowerInvariant().Trim();

        if (!SubdomainRegex.IsMatch(normalized))
            throw new DomainException("Subdomain format is invalid");

        if (IsReserved(normalized))
            throw new DomainException($"Subdomain '{normalized}' is reserved");

        return new SubDomain(normalized);
    }

    private static bool IsReserved(string subdomain)
    {
        var reserved = new[] { "www", "api", "admin", "app", "mail", "ftp", "cdn", "assets" };
        return reserved.Contains(subdomain);
    }

    public static implicit operator string(SubDomain subdomain) => subdomain.Value;
}