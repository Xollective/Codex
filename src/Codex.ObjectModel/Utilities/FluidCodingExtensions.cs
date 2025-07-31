namespace Codex.Sdk;

public static class FluidCodingExtensions
{
    public static TResult Then<T, TResult>(this T obj, TResult result)
        where T : notnull
    {
        return result;
    }

    public static bool IsAny<T>(this T value, ReadOnlySpan<T> values)
        where T : unmanaged, Enum
    {
        var comparer = EqualityComparer<T>.Default;
        foreach (var item in values)
        {
            if (comparer.Equals(item, value)) return true;
        }

        return false;
    }

    public static TResult? TryCast<TResult>(this object value)
        where TResult : class
    {
        return value as TResult;
    }

    public static bool TryGet<T>(this T? value, out T result)
        where T : struct
    {
        result = value.GetValueOrDefault();
        return value.HasValue;
    }

    public static TResult SelectOrDefault<T, TResult>(this T? value, Func<T, TResult> select, TResult defaultValue = default)
        where T : struct
    {
        if (value.HasValue)
        {
            return select(value.Value);
        }

        return defaultValue;
    }

    public static TResult Then<T, TResult>(this T value, Func<T, TResult> select)
    {
        return select(value);
    }

    public static T ValueOrDefault<T>(this bool isValid, T value, T defaultValue = default)
    {
        return isValid ? value : defaultValue;
    }

    public static Func<T, T> Join<T>(this Func<T, T> apply1, Func<T, T> apply2)
        where T : class
    {
        return o =>
        { 
            o = apply1?.Invoke(o) ?? o;
            o = apply2?.Invoke(o) ?? o;
            return o;
        };
    }

    public static Func<T, bool> And<T>(this Func<T, bool> include1, Func<T, bool> include2)
        where T : class
    {
        return o =>
        {
            var value = true;
            value &= include1?.Invoke(o) ?? value;
            value &= include2?.Invoke(o) ?? value;
            return value;
        };
    }

    public static TResult FluidSelect<T, TResult>(this T c, Func<T, TResult> selector)
    {
        return selector(c);
    }

    public static async ValueTask<TResult> FluidSelectAsync<T, TResult>(this ValueTask<T> c, Func<T, TResult> selector)
    {
        return selector(await c);
    }

    public static T FluidSelectIf<T>(this T c, Func<T, bool> condition, Func<T, T> selector)
    {
        return condition(c) ? selector(c) : c;
    }

    public static T Apply<T>(this T c, Action<T> apply)
    {
        apply(c);
        return c;
    }

    public static T Apply<T, TData>(this T c, TData data, Action<T, TData> apply)
    {
        apply(c, data);
        return c;
    }

    public static T Apply<T>(this Func<T, T> optionalApply, T value)
    {
        return optionalApply == null ? value : optionalApply(value);
    }

    public static Func<T, T> ApplyBefore<T>(this Func<T, T> before, Func<T, T> after)
    {
        if (before == null || after == null) return before ?? after;
        return value => after(before(value));
    }

    public static Action<T> ApplyBefore<T>(this Action<T> before, Action<T> after)
    {
        if (before == null || after == null) return before ?? after;
        return value =>
        {
            before(value);
            after(value);
        };
    }

    public static T ApplyIf<T>(this T c, bool shouldApply, Action<T> apply)
    {
        if (shouldApply)
        {
            apply(c);
        }

        return c;
    }
}