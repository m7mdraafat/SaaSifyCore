namespace SaaSifyCore.Api.DTOs;

/// <summary>
/// Standard API response wrapper for consistent response format.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null) => new()
    {
        Success = true,
        Message = message,
        Data = data
    };

    public static ApiResponse<T> FailureResponse(string message) => new()
    {
        Success = false,
        Message = message,
        Data = default
    };
}

/// <summary>
/// API response without data payload.
/// </summary>
public class ApiResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ApiResponse SuccessResponse(string message) => new()
    {
        Success = true,
        Message = message
    };

    public static ApiResponse FailureResponse(string message) => new()
    {
        Success = false,
        Message = message
    };
}
