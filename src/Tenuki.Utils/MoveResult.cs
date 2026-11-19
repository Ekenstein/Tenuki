namespace Tenuki.Utils;

public static class MoveResult
{
    public static MoveResult<T> Failure<T>(T origin) => new MoveResult<T>.Failure(origin);
    public static MoveResult<T> Success<T>(T position, T origin) => new MoveResult<T>.Success(position, origin);
}

public closed record MoveResult<T>(T Origin)
{
    public sealed record Failure(T Origin) : MoveResult<T>(Origin);
    public sealed record Success(T Position, T Origin) : MoveResult<T>(Origin);

    public MoveResult<T> Select<U>(Func<T, T> move) => SelectMany(x => MoveResult.Success(move(x), x));

    public MoveResult<U> Select<U>(U origin, Func<T, U> move) => SelectMany(origin, x => MoveResult.Success(move(x), origin));

    public MoveResult<U> SelectMany<U>(U origin, Func<T, MoveResult<U>> move) => this switch
    {
        Failure => MoveResult.Failure(origin),
        Success success => move(success.Position)
    };

    public MoveResult<T> SelectMany(Func<T, MoveResult<T>> move) => this switch
    {
        Failure failure => failure,
        Success success => move(success.Position)
    };
}

public static class MoveResultExtensions
{
    public static MoveResult<T> Or<T>(this MoveResult<T> result, Func<T, MoveResult<T>> move) => result switch
    {
        MoveResult<T>.Failure _ => move(result.Origin),
        MoveResult<T>.Success _ => result,
    };
}
