namespace SaaSifyCore.Domain.Common;

/// <summary>
/// Represents an error with a code and message.
/// </summary>
public sealed record Error
{
    public string Code { get; }
    public string Message { get; }

    public Error(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public static Error None => new(string.Empty, string.Empty);
    public static Error NullValue => new("Error.NullValue", "A null value was provided");

    public static implicit operator string(Error error) => error.Code;

    public override string ToString() => $"{Code}: {Message}";
}

/// <summary>
/// Common domain errors.
/// </summary>
public static class DomainErrors
{
    public static class User
    {
        public static Error EmailAlreadyExists => new(
            "User.EmailAlreadyExists",
            "The specified email is already in use");

        public static Error InvalidCredentials => new(
            "User.InvalidCredentials",
            "The email or password is incorrect");

        public static Error NotFound => new(
            "User.NotFound",
            "The user was not found");

        public static Error EmailNotVerified => new(
            "User.EmailNotVerified",
            "The email address has not been verified");

        public static Error AlreadyDeactivated => new(
            "User.AlreadyDeactivated",
            "The user is already deactivated");
    }

    public static class Tenant
    {
        public static Error NotFound => new(
            "Tenant.NotFound",
            "The tenant was not found");

        public static Error NotActive => new(
            "Tenant.NotActive",
            "The tenant is not active");

        public static Error SubdomainAlreadyExists => new(
            "Tenant.SubdomainAlreadyExists",
            "The specified subdomain is already in use");
    }

    public static class Auth
    {
        public static Error InvalidToken => new(
            "Auth.InvalidToken",
            "The provided token is invalid");

        public static Error TokenExpired => new(
            "Auth.TokenExpired",
            "The token has expired");

        public static Error RefreshTokenRevoked => new(
            "Auth.RefreshTokenRevoked",
            "The refresh token has been revoked");

        public static Error TenantContextNotResolved => new(
            "Auth.TenantContextNotResolved",
            "Tenant context could not be resolved");
    }

    public static class Subscription
    {
        public static Error NotFound => new(
            "Subscription.NotFound",
            "The subscription was not found");

        public static Error AlreadyActive => new(
            "Subscription.AlreadyActive",
            "The subscription is already active");

        public static Error AlreadyCancelled => new(
            "Subscription.AlreadyCancelled",
            "The subscription is already cancelled");

        public static Error CannotUpgrade => new(
            "Subscription.CannotUpgrade",
            "Cannot upgrade to a lower-tier plan");

        public static Error PastDue => new(
            "Subscription.PastDue",
            "The subscription has past due payments");
    }

    public static class Validation
    {
        public static Error Required(string fieldName) => new(
            "Validation.Required",
            $"{fieldName} is required");

        public static Error InvalidFormat(string fieldName) => new(
            "Validation.InvalidFormat",
            $"{fieldName} format is invalid");

        public static Error TooShort(string fieldName, int minLength) => new(
            "Validation.TooShort",
            $"{fieldName} must be at least {minLength} characters");

        public static Error TooLong(string fieldName, int maxLength) => new(
            "Validation.TooLong",
            $"{fieldName} must not exceed {maxLength} characters");
    }
}
