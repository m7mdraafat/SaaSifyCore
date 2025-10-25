using Microsoft.AspNetCore.Mvc;
using SaaSifyCore.Api.DTOs;
using SaaSifyCore.Domain.Common;

namespace SaaSifyCore.Api.Services;

/// <summary>
/// Service for mapping Result pattern errors to HTTP responses.
/// Follows Open/Closed Principle - extensible without modification.
/// </summary>
public interface IResultMapper
{
    /// <summary>
    /// Maps a Result to an IActionResult with appropriate HTTP status code.
    /// </summary>
    IActionResult MapToActionResult<T>(Result<T> result, Func<T, IActionResult> onSuccess);

    /// <summary>
    /// Maps a Result to an IActionResult with appropriate HTTP status code.
    /// </summary>
    IActionResult MapToActionResult(Result result, Func<IActionResult> onSuccess);
}
