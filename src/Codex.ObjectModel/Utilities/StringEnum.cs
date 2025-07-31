using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Codex.ObjectModel;

[GeneratorExclude]
public interface IStringEnum
{
    string StringValue { get; }

    int? IntegralValue { get; }
}

public interface IStringEnum<TSelf> : IStringEnum, IComparable<TSelf>, IEquatable<TSelf>
{
    static abstract TSelf FromString(string value);

    static abstract TSelf FromInteger(int value);
}

public static class StringEnum
{
    public static StringEnum<TEnum> ToStringEnum<TEnum>(this TEnum e)
        where TEnum : unmanaged, Enum, IConvertible
    {
        return e;
    }
}

public record struct StringEnum<TEnum> : IStringEnum<StringEnum<TEnum>>
    where TEnum : unmanaged, Enum, IConvertible
{
    public TEnum? Value { get; }
    public string StringValue { get; }

    public int? IntegralValue => Value?.ToInt32(null);

    public StringEnum(TEnum value)
    {
        Value = value;
        StringValue = value.ToString();
    }

    public TEnum ValueOrDefault(TEnum defaultValue = default)
    {
        return Value ?? defaultValue;
    }

    public StringEnum(string value)
    {
        var normalizedValue = Normalize(value, stackalloc char[100]);
        if (Enum.TryParse<TEnum>(normalizedValue, ignoreCase: true, out var result))
        {
            Value = result;
            StringValue = result.ToString();
        }
        else
        {
            Value = default;
            StringValue = value;
        }
    }

    public int CompareTo(StringEnum<TEnum> other)
    {
        return StringComparer.OrdinalIgnoreCase.Compare(StringValue, other.StringValue);
    }

    private ReadOnlySpan<char> Normalize(string value, Span<char> buffer)
    {
        if (value.Length <= buffer.Length)
        {
            buffer = buffer.Slice(0, value.Length);
            int normalizedLength = 0;
            for (int i = 0; i < value.Length; i++)
            {
                ref char bufferChar = ref buffer[normalizedLength];
                char valueChar = value[i];
                if (valueChar == '.')
                {
                    bufferChar = '_';
                    normalizedLength++;
                }
                else if (char.IsLetterOrDigit(valueChar))
                {
                    bufferChar = valueChar;
                    normalizedLength++;
                }
            }

            buffer = buffer.Slice(0, normalizedLength);

            return buffer;
        }

        return value;
    }

    public override string ToString()
    {
        return StringValue?.Replace("_", ".");
    }

    public string ToDisplayString()
    {
        return StringValue?.Replace("_", ".").ToLowerInvariant();
    }

    public static implicit operator StringEnum<TEnum>(string value)
    {
        return new StringEnum<TEnum>(value);
    }

    public static implicit operator StringEnum<TEnum>(Null value)
    {
        return default;
    }

    //public static implicit operator string(StringEnum<TEnum> value)
    //{
    //    return value.StringValue;
    //}

    private static readonly IEqualityComparer<TEnum> EqualityComparer = EqualityComparer<TEnum>.Default;

    public static implicit operator TEnum?(StringEnum<TEnum> value)
    {
        return value.Value;
    }

    public static implicit operator StringEnum<TEnum>(TEnum value)
    {
        return new StringEnum<TEnum>(value);
    }

    public static bool operator ==(StringEnum<TEnum> left, TEnum right)
    {
        return EqualityComparer.Equals(left.Value.GetValueOrDefault(), right);
    }

    public static bool operator !=(StringEnum<TEnum> left, TEnum right)
    {
        return !EqualityComparer.Equals(left.Value.GetValueOrDefault(), right);
    }

    public static bool operator ==(TEnum left, StringEnum<TEnum> right)
    {
        return EqualityComparer.Equals(right.Value.GetValueOrDefault(), left);
    }

    public static bool operator !=(TEnum left, StringEnum<TEnum> right)
    {
        return !EqualityComparer.Equals(right.Value.GetValueOrDefault(), left);
    }

    public static StringEnum<TEnum> FromString(string value)
    {
        return value;
    }

    public static StringEnum<TEnum> FromInteger(int value)
    {
        return Unsafe.As<int, TEnum>(ref value);
    }

    public class Null
    {
    }
}