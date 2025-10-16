using SaaSifyCore.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace SaaSifyCore.Domain.ValueObjects;

public record Email
{
    private static readonly Regex EmailRegex = new Regex(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Email cannot be empty");

        var normalized = value.ToLowerInvariant().Trim();

        if (!EmailRegex.IsMatch(normalized))
            throw new DomainException("Email format is invalid");

        return new Email(normalized);
    }

    public static implicit operator string(Email email) => email.Value;
}