using Medallion.Collections;

namespace Tenuki.Utils;

public sealed record Zipper<T>(ImmutableLinkedList<T> Left, T Focus, ImmutableLinkedList<T> Right);

public static class ZipperExtensions
{
    public static MoveResult<Zipper<T>> GoLeft<T>(this Zipper<T> zipper)
    {
        if (zipper.Left.Count == 0)
        {
            return MoveResult.Failure(zipper);
        }

        var (head, tail) = zipper.Left;
        var move = zipper with
        {
            Left = tail,
            Focus = head,
            Right = LinkedListOf([zipper.Focus, .. zipper.Right]),
        };

        return MoveResult.Success(move, zipper);
    }

    public static MoveResult<Zipper<T>> GoRight<T>(this Zipper<T> zipper)
    {
        if (zipper.Right.Count == 0)
        {
            return MoveResult.Failure(zipper);
        }

        var (head, tail) = zipper.Right;
        var move = zipper with
        {
            Left = LinkedListOf([zipper.Focus, .. zipper.Left]),
            Focus = head,
            Right = tail,
        };

        return MoveResult.Success(move, zipper);
    }

    public static Zipper<T> GoToLast<T>(this Zipper<T> zipper) =>
        zipper.GoRight() switch
        {
            MoveResult<Zipper<T>>.Failure => zipper,
            MoveResult<Zipper<T>>.Success success => success.Position.GoToLast(),
        };

    public static ImmutableLinkedList<T> CommitAtCurrentPosition<T>(this Zipper<T> zipper) =>
        zipper.Left.Reverse([zipper.Focus]);
}
