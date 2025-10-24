using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaSifyCore.Api.DTOs.Auth;
using SaaSifyCore.Application.Commands.Auth;
using SaaSifyCore.Domain.Interfaces;
using System.Security.Claims;

namespace SaaSifyCore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IApplicationDbContext _context;
        private readonly ITenantContext _tenantContext;

        public AuthController(
            IMediator mediator,
            IApplicationDbContext context,
            ITenantContext tenantContext)
        {
            _mediator = mediator;
            _context = context;
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

            // Create command
            var command = new RegisterCommand(
                Email: request.Email,
                Password: request.Password,
                FirstName: request.FirstName,
                LastName: request.LastName,
                TenantId: _tenantContext.TenantId!.Value
            );

            // Send command via MediatR
            var result = await _mediator.Send(command);

            // Handle result with Result pattern
            if (result.IsFailure)
            {
                return result.Error.Code switch
                {
                    "User.EmailAlreadyExists" => Conflict(new { message = result.Error.Message }),
                    "Tenant.NotFound" => BadRequest(new { message = result.Error.Message }),
                    "Tenant.NotActive" => BadRequest(new { message = result.Error.Message }),
                    var code when code.StartsWith("Validation") => BadRequest(new { message = result.Error.Message }),
                    _ => BadRequest(new { message = result.Error.Message })
                };
            }

            var authResponse = result.Value;
            
            return CreatedAtAction(nameof(GetCurrentUser), new { id = authResponse.User.Id }, authResponse);
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

            // Create command
            var command = new LoginCommand(
                Email: request.Email,
                Password: request.Password,
                TenantId: _tenantContext.TenantId!.Value
            );

            // Send command via MediatR
            var result = await _mediator.Send(command);

            // Handle result with Result pattern
            if (result.IsFailure)
            {
                return result.Error.Code switch
                {
                    "User.InvalidCredentials" => Unauthorized(new { message = result.Error.Message }),
                    "User.EmailNotVerified" => Unauthorized(new { message = result.Error.Message }),
                    _ => BadRequest(new { message = result.Error.Message })
                };
            }

            var authResponse = result.Value;

            return Ok(authResponse);
        }

        /// <summary>
        /// Refresh access token using a valid refresh token.
        /// </summary>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequest request)
        {
            if (!_tenantContext.IsResolved)
            {
                return BadRequest(new {message = "Tenant context not resolved."});
            }

            // Create command
            var command = new RefreshTokenCommand(
                RefreshToken: request.RefreshToken,
                TenantId: _tenantContext.TenantId!.Value
            );

            // Send command via MediatR
            var result = await _mediator.Send(command);

            // Handle result with Result pattern
            if (result.IsFailure)
            {
                return result.Error.Code switch
                {
                    "Auth.InvalidToken" => Unauthorized(new { message = result.Error.Message }),
                    "Auth.TokenExpired" => Unauthorized(new { message = result.Error.Message }),
                    "Auth.RefreshTokenRevoked" => Unauthorized(new { message = result.Error.Message }),
                    _ => BadRequest(new { message = result.Error.Message })
                };
            }

            var authResponse = result.Value;

            return Ok(authResponse);
        }

        /// <summary>
        /// Logout (revoke refresh token).
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            if (!_tenantContext.IsResolved)
            {
                return BadRequest(new {message = "Tenant context not resolved."});
            }

            // Create command
            var command = new LogoutCommand(
                RefreshToken: request.RefreshToken,
                TenantId: _tenantContext.TenantId!.Value
            );

            // Send command via MediatR
            var result = await _mediator.Send(command);

            // Handle result with Result pattern
            if (result.IsFailure)
            {
                return result.Error.Code switch
                {
                    "Auth.InvalidToken" => NotFound(new { message = result.Error.Message }),
                    _ => BadRequest(new { message = result.Error.Message })
                };
            }

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
                (
                    u.Id,
                    u.Email.Value,
                    u.FirstName,
                    u.LastName,
                    u.Role.ToString()
                ))
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return userDto;
        }
    }
}
