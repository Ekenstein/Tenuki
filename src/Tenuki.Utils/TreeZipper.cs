using Medallion.Collections;

namespace Tenuki.Utils;

public interface IUnzip<T>
{
    ImmutableLinkedList<T> Unzip(T node);
    T Zip(T node, ImmutableLinkedList<T> children);
}

public static class TreeZipper
{
    public static TreeZipper<T> FromNode<T>(T node, IUnzip<T> unzip) =>
        new(Left: [], Focus: node, Right: [], Top: [], Unzip: unzip);
}

public sealed record TreeZipper<T>(
    ImmutableLinkedList<T> Left,
    T Focus,
    ImmutableLinkedList<T> Right,
    ImmutableLinkedList<(ImmutableLinkedList<T>, T, ImmutableLinkedList<T>)> Top,
    IUnzip<T> Unzip
);

public static class TreeZipperExtensions
{
    public static MoveResult<TreeZipper<T>> GoLeft<T>(this TreeZipper<T> zipper)
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
            Right = [zipper.Focus, .. zipper.Right],
        };

        return MoveResult.Success(move, zipper);
    }

    public static MoveResult<TreeZipper<T>> GoRight<T>(this TreeZipper<T> zipper)
    {
        if (zipper.Right.Count == 0)
        {
            return MoveResult.Failure(zipper);
        }

        var (head, tail) = zipper.Right;

        var move = zipper with
        {
            Left = [zipper.Focus, .. zipper.Left],
            Focus = head,
            Right = tail,
        };

        return MoveResult.Success(move, zipper);
    }

    public static MoveResult<TreeZipper<T>> GoUp<T>(this TreeZipper<T> zipper)
    {
        if (zipper.Top.Count == 0)
        {
            return MoveResult.Failure(zipper);
        }

        var (head, tail) = zipper.Top;
        var children = ImmutableLinkedList
            .CreateRange([zipper.Focus, .. zipper.Left])
            .Reverse(zipper.Right);
        var (topLeft, topFocus, topRight) = head;

        var move = zipper with
        {
            Left = topLeft,
            Focus = zipper.Unzip.Zip(topFocus, children),
            Right = topRight,
            Top = tail,
        };

        return MoveResult.Success(move, zipper);
    }

    public static MoveResult<TreeZipper<T>> GoDownLeft<T>(this TreeZipper<T> zipper)
    {
        var children = zipper.Unzip.Unzip(zipper.Focus);
        if (children.Count == 0)
        {
            return MoveResult.Failure(zipper);
        }

        var (head, tail) = children;

        var move = zipper with
        {
            Left = [],
            Focus = head,
            Right = tail,
            Top = [(zipper.Left, zipper.Focus, zipper.Right), .. zipper.Top],
        };

        return MoveResult.Success(move, zipper);
    }

    public static TreeZipper<T> GoToRoot<T>(this TreeZipper<T> zipper) =>
        zipper.GoUp() switch
        {
            MoveResult<TreeZipper<T>>.Success success => success.Position.GoToRoot(),
            MoveResult<TreeZipper<T>>.Failure _ => zipper,
        };
}
