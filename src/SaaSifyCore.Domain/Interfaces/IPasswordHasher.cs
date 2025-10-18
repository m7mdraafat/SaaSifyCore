namespace SaaSifyCore.Domain.Interfaces;

/// <summary>
/// Abstracts password hashing logic (BCrypt).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain-text password.
    /// </summary>
    /// <param name="password">The plain-text password.</param>
    /// <returns>The hashed password.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Hashes a plain-text password asynchronously.
    /// </summary>
    /// <param name="password">The plain-text password</param>
    /// <returns>The hashed password.</returns>
    Task<string> HashPasswordAsync(string password);

    /// <summary>
    /// Verifies a plain-text password against a hashed password.
    /// </summary>
    /// <param name="password">The plain-text password.</param>
    /// <param name="hashedPassword">The hashed password.</param>
    /// <returns>True if the password is valid; otherwise, false.</returns>
    bool VerifyPassword(string password, string hashedPassword);

    /// <summary>
    /// Verifies a plain-text password against a hashed password asynchronously.
    /// </summary>
    /// <param name="password">The plain-text password.</param>
    /// <param name="hashedPassword">The hashed password.</param>
    /// <returns>True if the password is valid; otherwise, false.</returns>
    Task<bool> VerifyPasswordAsync(string password, string hashedPassword);
}