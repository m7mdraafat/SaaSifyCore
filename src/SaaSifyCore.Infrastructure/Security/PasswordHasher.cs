using SaaSifyCore.Domain.Interfaces;

namespace SaaSifyCore.Infrastructure.Security;

/// <summary>
/// BCrypt implementation of password hashing.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; // 2^12 iterations (industry standard)

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public async Task<string> HashPasswordAsync(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        return await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor));
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (string.IsNullOrWhiteSpace(hashedPassword))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> VerifyPasswordAsync(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
            return false;

        try
        {
            return await Task.Run(() => BCrypt.Net.BCrypt.Verify(password, hashedPassword));
        }
        catch
        {
            return false;
        }
    }
}