
using Codex.Utilities.Serialization;

namespace Codex.ObjectModel;

public record struct RepoName(string Value) : IStringConvertible<RepoName>, IEquatable<RepoName>
{
    public static RepoName ConvertFromJson(string jsonFormat)
    {
        return new RepoName(jsonFormat);
    }

    public string ConvertToJson()
    {
        return Value;
    }

    public bool Equals(RepoName other)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    public static implicit operator RepoName(string value)
    {
        return new(value);
    }

    public static implicit operator string(RepoName value)
    {
        return value.Value;
    }
}