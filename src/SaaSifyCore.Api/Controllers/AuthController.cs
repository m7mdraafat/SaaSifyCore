using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaSifyCore.Api.DTOs.Auth;
using SaaSifyCore.Domain.Entities;
using SaaSifyCore.Domain.Enums;
using SaaSifyCore.Domain.Interfaces;
using SaaSifyCore.Domain.ValueObjects;
using System.Security.Claims;

namespace SaaSifyCore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly ITenantContext _tenantContext;

        public AuthController(
            IApplicationDbContext context,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            ITenantContext tenantContext)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Register a new user for the current tenant.
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request)
        {
            if (!_tenantContext.IsResolved)
            {
                return BadRequest(new {message = "Tenant context not resolved."});
            }

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email && u.TenantId == _tenantContext.TenantId);

            if (emailExists)
            {
                return Conflict(new { message = "Email already in use for this tenant." });
            }

            string hashedPassword = await _passwordHasher.HashPasswordAsync(request.Password);

            var user = SaaSifyCore.Domain.Entities.User.Create(
                Email.Create(request.Email),
                hashedPassword,
                request.FirstName,
                request.LastName,
                UserRole.User,
                _tenantContext.TenantId!.Value);

            await _context.Users.AddAsync(user);
            
            string jwtToken = _jwtTokenGenerator.GenerateToken(user, _tenantContext.TenantId.Value);
            RefreshToken refreshToken = RefreshToken.Create(user.Id);
            await _context.RefreshTokens.AddAsync(refreshToken);
            
            await _context.SaveChangesAsync();

            var response = new AuthResponse
            {
                AccessToken = jwtToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email.Value,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString()
                }
            };
            
            return CreatedAtAction(nameof(GetCurrentUser), new { id = user.Id }, response);
        }

        /// <summary>
        /// Login with email and password for the current tenant.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
        {
            if (!_tenantContext.IsResolved)
            {
                return BadRequest(new {message = "Tenant context not resolved."});
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == _tenantContext.TenantId);

            string dummyHash = "$2a$12$dummy.hash.for.timing.constant.protection";
            string hashToVerify = user?.PasswordHash ?? dummyHash;
            bool isValid = await _passwordHasher.VerifyPasswordAsync(request.Password, hashToVerify);

            if (user is null || !isValid)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var existingTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();

            // Keep only the 4 most recent tokens
            if (existingTokens.Count >= 4)
            {
                var tokensToRevoke = existingTokens.Skip(3);
                foreach (var token in tokensToRevoke)
                {
                    token.Revoke();
                }
            }
            
            string accessToken = _jwtTokenGenerator.GenerateToken(user, _tenantContext.TenantId!.Value);
            RefreshToken refreshToken = RefreshToken.Create(user.Id);
            await _context.RefreshTokens.AddAsync(refreshToken);

            await _context.SaveChangesAsync();

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email.Value,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString()
                }
            };
            return Ok(response);
        }

        /// <summary>
        /// Refresh access token using a valid refresh token.
        /// </summary>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequest request)
        {
            var refreshToken = await _context.RefreshTokens
                .IgnoreQueryFilters()
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshToken is null ||
                refreshToken.User.TenantId != _tenantContext.TenantId)
            {
                // TODO: Log security event if tenant mismatch
                return Unauthorized(new { message = "Invalid refresh token." });
            }

            if (refreshToken.IsRevoked)
            {
                return Unauthorized(new { message = "Refresh token has been revoked." });
            }

            if (refreshToken.ExpiresAt <= DateTime.UtcNow)
            {
                return Unauthorized(new { message = "Refresh token has expired." });
            }
            
            var user = refreshToken.User;

            // Revoke old token
            refreshToken.Revoke();

            // Generate new tokens.
            var newAccessToken = _jwtTokenGenerator.GenerateToken(user, _tenantContext.TenantId!.Value);
            var newRefreshToken = RefreshToken.Create(user.Id);

            await _context.RefreshTokens.AddAsync(newRefreshToken);
            await _context.SaveChangesAsync();

            var response = new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email.Value,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString()
                }
            };

            return Ok(response);
        }

        /// <summary>
        /// Logout (revoke refresh token).
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshToken is null)
            {
                return NotFound(new {message = "Refresh token not found."});
            }

            refreshToken.Revoke();
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Get current user info.
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            UserDto? userDto = await this.GetUserDto(userId);

            if (userDto is null)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(userDto);
        }


        private async Task<UserDto?> GetUserDto(Guid userId)
        {
            UserDto? userDto = await _context.Users
                .Where(u => u.Id == userId && u.TenantId == _tenantContext.TenantId)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email.Value,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role.ToString(),
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return userDto;
        }
    }
}
