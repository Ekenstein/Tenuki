namespace Tenuki.Utils;

public static class ObjectExtensions
{
    public static T Apply<T>(this T obj, Action<T> action)
    {
        action(obj);
        return obj;
    }

    public static U Let<T, U>(this T obj, Func<T, U> func) => func(obj);

    public static void Also<T>(this T obj, Action<T> action) => action(obj);
}
