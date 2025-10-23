namespace SaaSifyCore.Domain.Common;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Successful result cannot have an error");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failed result must have an error");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    // Convenience method for string errors (backward compatibility)
    public static Result Failure(string errorMessage) => 
        new(false, new Error("Error", errorMessage));

    public static Result<T> Success<T>(T value) => new(value, true, Error.None);

    public static Result<T> Failure<T>(Error error) => new(default!, false, error);

    public static Result<T> Failure<T>(string errorMessage) => 
        new(default!, false, new Error("Error", errorMessage));
}

/// <summary>
/// Represents the result of an operation that returns a value.
/// </summary>
/// <typeparam name="T">The type of the value returned by the operation.</typeparam>
public class Result<T> : Result
{
    public T Value { get; }

    protected internal Result(T value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);
}
