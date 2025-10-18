using SaaSifyCore.Domain.Common;
using System.Security.Cryptography;

namespace SaaSifyCore.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    // EF Core constructor
    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, int expiryDays = 30)
    {
        var tokenBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        var token = Convert.ToBase64String(tokenBytes);

        var refreshToken = new RefreshToken
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            UserId = userId,
            IsRevoked = false
        };

        return refreshToken;
    }

    public bool IsValid() => !IsRevoked && ExpiresAt > DateTime.UtcNow;

    public void Revoke()
    {
        IsRevoked = true;
    }
}