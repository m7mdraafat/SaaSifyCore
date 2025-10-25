using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaSifyCore.Api.Configuration;
using SaaSifyCore.Api.DTOs;
using SaaSifyCore.Api.DTOs.Auth;
using SaaSifyCore.Api.Services;
using SaaSifyCore.Application.Commands.Auth;
using SaaSifyCore.Application.Queries.Auth;
using SaaSifyCore.Domain.Interfaces;
using System.Security.Claims;

namespace SaaSifyCore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ITenantContext _tenantContext;
        private readonly IRefreshTokenCookieService _cookieService;
        private readonly IResultMapper _resultMapper;

        public AuthController(
            IMediator mediator,
            ITenantContext tenantContext,
            IRefreshTokenCookieService cookieService,
            IResultMapper resultMapper)
        {
            _mediator = mediator;
            _tenantContext = tenantContext;
            _cookieService = cookieService;
            _resultMapper = resultMapper;
        }

        /// <summary>
        /// Register a new user for the current tenant.
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request)
        {
            if (!_tenantContext.IsResolved)
            {
                return BadRequest(ApiResponse.FailureResponse("Tenant context not resolved."));
            }

            var command = new RegisterCommand(
                Email: request.Email,
                Password: request.Password,
                FirstName: request.FirstName,
                LastName: request.LastName,
                TenantId: _tenantContext.TenantId!.Value
            );

            var result = await _mediator.Send(command);

            return _resultMapper.MapToActionResult(
                result,
                onSuccess: () => StatusCode(
                    StatusCodes.Status201Created,
                    ApiResponse.SuccessResponse("Account created. Please verify your email.")));
        }

        /// <summary>
        /// Login with email and password for the current tenant.
        /// Returns access token in response and refresh token in HTTP-only cookie.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
        {
            if (!_tenantContext.IsResolved)
            {
                return BadRequest(ApiResponse.FailureResponse("Tenant context not resolved."));
            }

            var command = new LoginCommand(
                Email: request.Email,
                Password: request.Password,
                TenantId: _tenantContext.TenantId!.Value
            );

            var result = await _mediator.Send(command);

            return _resultMapper.MapToActionResult(
                result,
                onSuccess: authResponse =>
                {
                    // Set refresh token in secure cookie
                    _cookieService.SetRefreshTokenCookie(
                        authResponse.RefreshToken,
                        authResponse.ExpiresAt.AddDays(AuthConstants.RefreshTokenExpirationDays));

                    // Return only access token
                    var loginResponse = new LoginResponse(
                        AccessToken: authResponse.AccessToken,
                        ExpiresIn: AuthConstants.AccessTokenExpirationSeconds
                    );

                    return Ok(ApiResponse<LoginResponse>.SuccessResponse(loginResponse));
                });
        }

        /// <summary>
        /// Refresh access token using refresh token from cookie.
        /// Rotates refresh token on every use (security best practice).
        /// </summary>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshTokenAsync()
        {
            if (!_tenantContext.IsResolved)
            {
                return BadRequest(ApiResponse.FailureResponse("Tenant context not resolved."));
            }

            var refreshToken = _cookieService.GetRefreshTokenFromCookie();
            if (refreshToken is null)
            {
                return Unauthorized(ApiResponse.FailureResponse("Invalid or expired refresh token."));
            }

            var command = new RefreshTokenCommand(
                RefreshToken: refreshToken,
                TenantId: _tenantContext.TenantId!.Value
            );

            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                _cookieService.DeleteRefreshTokenCookie();
            }

            return _resultMapper.MapToActionResult(
                result,
                onSuccess: authResponse =>
                {
                    // Rotate refresh token (set new one)
                    _cookieService.SetRefreshTokenCookie(
                        authResponse.RefreshToken,
                        authResponse.ExpiresAt.AddDays(AuthConstants.RefreshTokenExpirationDays));

                    var loginResponse = new LoginResponse(
                        AccessToken: authResponse.AccessToken,
                        ExpiresIn: AuthConstants.AccessTokenExpirationSeconds
                    );

                    return Ok(ApiResponse<LoginResponse>.SuccessResponse(loginResponse));
                });
        }

        /// <summary>
        /// Logout - revoke refresh token and clear cookie.
        /// No JWT required - validates refresh token from cookie.
        /// </summary>
        [HttpPost("logout")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Logout()
        {
            if (!_tenantContext.IsResolved)
            {
                return BadRequest(ApiResponse.FailureResponse("Tenant context not resolved."));
            }

            var refreshToken = _cookieService.GetRefreshTokenFromCookie();
            if (refreshToken is not null)
            {
                var command = new LogoutCommand(
                    RefreshToken: refreshToken,
                    TenantId: _tenantContext.TenantId!.Value
                );

                // Send command (ignore result - logout is idempotent)
                await _mediator.Send(command);
            }

            // Always clear the cookie
            _cookieService.DeleteRefreshTokenCookie();

            return Ok(ApiResponse.SuccessResponse("Logout successful."));
        }

        /// <summary>
        /// Get current authenticated user info.
        /// Validates tenant claim matches request tenant.
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = GetUserIdFromClaims();
            if (userId is null)
            {
                return Unauthorized(ApiResponse.FailureResponse("Invalid or expired access token."));
            }

            if (!_tenantContext.IsResolved)
            {
                return BadRequest(ApiResponse.FailureResponse("Tenant context not resolved."));
            }

            var query = new GetCurrentUserQuery(
                UserId: userId.Value,
                TenantId: _tenantContext.TenantId!.Value
            );

            var result = await _mediator.Send(query);

            return _resultMapper.MapToActionResult(
                result,
                onSuccess: userDto => Ok(ApiResponse<UserDto>.SuccessResponse(userDto)));
        }

        private Guid? GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}
