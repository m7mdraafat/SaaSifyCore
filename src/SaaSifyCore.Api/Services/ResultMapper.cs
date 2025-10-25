using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaSifyCore.Api.DTOs;
using SaaSifyCore.Domain.Common;

namespace SaaSifyCore.Api.Services;

/// <summary>
/// Maps domain Result pattern to HTTP responses with appropriate status codes.
/// Centralizes error-to-HTTP mapping logic.
/// </summary>
public class ResultMapper : IResultMapper
{
    public IActionResult MapToActionResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value);
        }

        return MapErrorToActionResult(result.Error);
    }

    public IActionResult MapToActionResult(Result result, Func<IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess();
        }

        return MapErrorToActionResult(result.Error);
    }

    private static IActionResult MapErrorToActionResult(Error error)
    {
        var (statusCode, message) = error.Code switch
        {
            // Authentication errors
            "User.InvalidCredentials" => (StatusCodes.Status401Unauthorized, "Invalid credentials."),
            "User.NotFound" => (StatusCodes.Status401Unauthorized, "Invalid credentials."), // Generic for security
            "Auth.InvalidRefreshToken" => (StatusCodes.Status401Unauthorized, "Invalid or expired refresh token."),
            "Auth.RefreshTokenExpired" => (StatusCodes.Status401Unauthorized, "Invalid or expired refresh token."),
            "Auth.RefreshTokenRevoked" => (StatusCodes.Status401Unauthorized, "Invalid or expired refresh token."),

            // Authorization/Forbidden errors
            "User.EmailNotVerified" => (StatusCodes.Status403Forbidden, "Please verify your email before logging in."),
            "Tenant.NotActive" => (StatusCodes.Status403Forbidden, error.Message),
            "User.TenantMismatch" => (StatusCodes.Status403Forbidden, "User does not belong to this tenant."),

            // Conflict errors
            "User.EmailAlreadyExists" => (StatusCodes.Status409Conflict, "Email is already registered."),

            // Not found errors
            "Tenant.NotFound" => (StatusCodes.Status404NotFound, error.Message),
            "User.UserNotFound" => (StatusCodes.Status404NotFound, "User not found."),

            // Validation errors
            var code when code.StartsWith("Validation") => (StatusCodes.Status400BadRequest, error.Message),

            // Default to BadRequest
            _ => (StatusCodes.Status400BadRequest, error.Message)
        };

        return new ObjectResult(ApiResponse.FailureResponse(message))
        {
            StatusCode = statusCode
        };
    }
}
