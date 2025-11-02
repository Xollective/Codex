namespace Codex.Utilities.Serialization;

public record struct NormalizedPath : IEquatable<NormalizedPath>, ISelectComparable<NormalizedPath, string>, IStringConvertible<NormalizedPath>
{
    public const char SeparatorChar = '/';

    public NormalizedPath(string path)
        : this(path, deserialized: false)
    {
    }

    private NormalizedPath(string path, bool deserialized)
    {
        Path = deserialized ? path : PathUtilities.NormalizePath(path, SeparatorChar);
    }

    public override string ToString()
    {
        return Path;
    }

    public string[] Split() => Path.Split(SeparatorChar);

    public string Extension => Path.AsSpan().SubstringAfterLastIndexOfAny("/").SubstringAfterLastIndexOfAny(".", requireMatch: true).ToString();

    public int Length => Path.Length;

    public string Path { get; }

    public ReadOnlySpan<char> AsSpan() => Path.AsSpan();

    public static NormalizedPath ConvertFromJson(string jsonFormat)
    {
        return new(jsonFormat, deserialized: true);
    }

    public string ConvertToJson()
    {
        return Path;
    }

    public string SelectComparable()
    {
        return Path;
    }

    public static implicit operator NormalizedPath(string value)
    {
        return value == null ? default : new NormalizedPath(value);
    }

    public static implicit operator ReadOnlySpan<char>(NormalizedPath value) => value.Path;

    public static implicit operator string(NormalizedPath value) => value.Path;
}