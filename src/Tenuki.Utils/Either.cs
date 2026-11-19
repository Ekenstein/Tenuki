namespace Tenuki.Utils;

public sealed record Left<T>(T Value);
public sealed record Right<T>(T Value);

public readonly union Either<L, R>(Left<L>, Right<R>);
