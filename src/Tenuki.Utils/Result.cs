using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Tenuki.Utils;

public sealed record Success<T>(T Value);
public sealed record Error(string Code, string Message);
public sealed record Failure(ImmutableList<Error> Errors);

public static class Result
{
    public static Result<T> Success<T>(T value) => new Success<T>(value);
    public static Result<U> Select<T, U>(this Result<T> result, Func<T, U> mapper) => result switch
    {
        Success<T> success => new Success<U>(mapper(success.Value)),
        Failure failure => failure,
    };

    public static Result<U> SelectMany<T, U>(this Result<T> result, Func<T, Result<U>> mapper) =>
        result switch
        {
            Success<T> success => mapper(success.Value),
            Failure failure => failure
        };

    public static Result<V> SelectMany<T, U, V>(
        this Result<T> result,
        Func<T, Result<U>> selector,
        Func<T, U, V> resultSelector) =>
        result.SelectMany(t =>
            selector(t).Select(u =>
                resultSelector(t, u)
            )
        );

    public static Result<T> Ensure<T>(
        this Result<T> result,
        Predicate<T> predicate,
        Error error
    )
    {
        return result.SelectMany(Ensure);
        Result<T> Ensure(T value) => predicate(value) ? new Success<T>(value) : new Failure([error]);
    }

    public static (ImmutableList<Error> errors, ImmutableList<T> values) Partition<T>(this IEnumerable<Result<T>> results)
    {
        var errors = MutableListOf<Error>();
        var values = MutableListOf<T>();

        foreach (var result in results)
        {
            switch (result)
            {
                case Success<T> success: values.Add(success.Value); break;
                case Failure failure: errors.AddRange(failure.Errors); break;
            }
        }

        return (errors.ToImmutable(), values.ToImmutable());
    }
}

public readonly union Result<T>(Success<T>, Failure)
{
    public ImmutableList<Error> Errors => this switch
    {
        Failure failure => failure.Errors,
        _ => []
    };

    public bool IsSuccess => this switch
    {
        Success<T> => true,
        _ => false
    };

    public T ValueOrThrow(Func<ImmutableList<Error>, Exception>? exceptionSelector = null) => TryGetValue(out var value)
        ? value
        : throw exceptionSelector?.Invoke(Errors) 
            ?? new InvalidOperationException("The result represents a failure.");

    public T? ValueOrDefault() => TryGetValue(out var value)
        ? value
        : default;

    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        if (this is Success<T> success)
        {
            value = success.Value;
            return true;
        }

        value = default;
        return false;
    }
};