namespace SaaSifyCore.Domain.Common;

/// <summary>
/// Extension methods for Result pattern.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Executes the specified action if the result is successful.
    /// </summary>
    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Executes the specified action if the result is a failure.
    /// </summary>
    public static Result<T> OnFailure<T>(this Result<T> result, Action<Error> action)
    {
        if (result.IsFailure)
        {
            action(result.Error);
        }

        return result;
    }

    /// <summary>
    /// Transforms the value of a successful result.
    /// </summary>
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper)
    {
        return result.IsSuccess
            ? Result.Success(mapper(result.Value))
            : Result.Failure<TOut>(result.Error);
    }

    /// <summary>
    /// Binds the result to another operation that returns a Result.
    /// </summary>
    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> binder)
    {
        return result.IsSuccess
            ? binder(result.Value)
            : Result.Failure<TOut>(result.Error);
    }

    /// <summary>
    /// Returns the value if successful, otherwise returns the specified default value.
    /// </summary>
    public static T GetValueOrDefault<T>(this Result<T> result, T defaultValue = default!)
    {
        return result.IsSuccess ? result.Value : defaultValue;
    }

    /// <summary>
    /// Matches the result to one of two functions based on success or failure.
    /// </summary>
    public static TOut Match<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> onSuccess,
        Func<Error, TOut> onFailure)
    {
        return result.IsSuccess
            ? onSuccess(result.Value)
            : onFailure(result.Error);
    }

    /// <summary>
    /// Combines multiple results into a single result.
    /// Returns the first failure, or success if all succeed.
    /// </summary>
    public static Result Combine(params Result[] results)
    {
        foreach (var result in results)
        {
            if (result.IsFailure)
                return result;
        }

        return Result.Success();
    }

    /// <summary>
    /// Ensures a condition is met, otherwise returns a failure.
    /// </summary>
    public static Result<T> Ensure<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        Error error)
    {
        if (result.IsFailure)
            return result;

        return predicate(result.Value)
            ? result
            : Result.Failure<T>(error);
    }

    /// <summary>
    /// Converts an async Result operation.
    /// </summary>
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, TOut> mapper)
    {
        var result = await resultTask;
        return result.Map(mapper);
    }

    /// <summary>
    /// Binds an async Result operation.
    /// </summary>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Task<Result<TOut>>> binder)
    {
        var result = await resultTask;
        return result.IsSuccess
            ? await binder(result.Value)
            : Result.Failure<TOut>(result.Error);
    }
}
