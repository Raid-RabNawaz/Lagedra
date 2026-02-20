namespace Lagedra.SharedKernel.Results;

/// <summary>Non-generic result for commands that return no value.</summary>
public sealed class Result
{
    private readonly Error? _error;

    private Result() { IsSuccess = true; }
    private Result(Error error) { _error = error; IsSuccess = false; }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error => _error ?? Error.None;

    public static Result Success() => new();
    public static Result Failure(Error error) => new(error);

    /// <summary>Named alternate for the implicit Error → Result conversion (satisfies CA2225).</summary>
    public static Result FromError(Error error) => Failure(error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Generic result carrying a value on success.</summary>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        _value = value;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        _error = error;
        IsSuccess = false;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result.");

    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful result.");

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>Named alternate for T → Result&lt;T&gt; implicit conversion (satisfies CA2225).</summary>
    public static Result<T> FromValue(T value) => Success(value);

    /// <summary>Named alternate for Error → Result&lt;T&gt; implicit conversion (satisfies CA2225).</summary>
    public static Result<T> FromError(Error error) => Failure(error);

    /// <summary>Converts this result to a <see cref="Result{T}"/> (identity; satisfies CA2225).</summary>
    public Result<T> ToResult() => this;

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(_value!) : onFailure(_error!);
    }
}
