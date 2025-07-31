using System;
using System.Diagnostics.ContractsLight;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using Codex.Utilities.Serialization;

namespace Codex.ObjectModel;

[DataContract]
public record struct SourceEncodingInfo(
    string Name,
    [property: DataMember] int PreambleLength) : IJsonConvertible<SourceEncodingInfo, EncodingName>
{
    private static Encoding utf32BEEncoding;

    public static readonly SourceEncodingInfo Default = FromEncoding(Encoding.UTF8, 0);

    private EncodingName EncodingName { get; } = GetEncodingName(Name, PreambleLength);

    [DataMember]
    public string Name { get; } = GetEncodingName(Name, PreambleLength).ToString();

    [JsonIgnore]
    [IgnoreDataMember]
    public Encoding Encoding => GetEncoding(EncodingName);

    [JsonIgnore]
    [IgnoreDataMember]
    public ReadOnlySpan<byte> EffectivePreamble => PreambleLength == 0 ? Array.Empty<byte>() : Encoding.Preamble;

    public static SourceEncodingInfo FromString(string name, int preambleLength)
    {
        EncodingName encodingName = GetEncodingName(name, preambleLength);
        var encoding = GetEncoding(encodingName);

        Contract.Assert(preambleLength == encoding.Preamble.Length);
        return new SourceEncodingInfo(name, preambleLength);
    }

    public override string ToString()
    {
        return Name;
    }

    public static SourceEncodingInfo FromEncoding(Encoding encoding, int? preambleLength = null)
    {
        return new SourceEncodingInfo(encoding.WebName, preambleLength ?? encoding.Preamble.Length);
    }

    private static EncodingName GetEncodingName(string name, int preambleLength)
    {
        Contract.Assert(name.Length < 40);
        Span<char> chars = stackalloc char[name.Length];
        name.CopyTo(chars);
        chars.Replace('-', '_');
        var encodingName = Enum.Parse<EncodingName>(chars);
        if (preambleLength == 0 && !chars.EndsWith("nobom") && encodingName != EncodingName.us_ascii)
        {
            encodingName--;
        }

        return encodingName;
    }

    public static implicit operator EncodingName(SourceEncodingInfo info)
    {
        return GetEncodingName(info.Name, info.PreambleLength);
    }

    public static implicit operator SourceEncodingInfo(EncodingName value)
    {
        return ConvertFromJson(value);
    }

    private static Encoding[] s_encodingCache = new Encoding[Enum.GetValues<EncodingName>().Length];

    private static Encoding GetEncoding(EncodingName name)
    {
        Encoding get(Encoding standard, bool bom, Func<Encoding> create)
        {
            if (standard == null || (standard.Preamble.Length != 0) != bom)
            {
                return s_encodingCache[(int)name] ??= create();
            }

            return standard;
        }

        switch (name)
        {
            case EncodingName.utf_8:
                return get(Encoding.UTF8, true, () => new UTF8Encoding(true));
            case EncodingName.utf_8_nobom:
                return get(Encoding.UTF8, false, () => new UTF8Encoding(false));
            case EncodingName.us_ascii:
            case EncodingName.us_ascii_nobom:
                return Encoding.ASCII;
            case EncodingName.utf_16:
                return get(Encoding.Unicode, true, () => new UnicodeEncoding(false, true));
            case EncodingName.utf_16_nobom:
                return get(Encoding.Unicode, false, () => new UnicodeEncoding(false, false));
            case EncodingName.utf_16be:
                return get(Encoding.BigEndianUnicode, true, () => new UnicodeEncoding(true, true));
            case EncodingName.utf_16be_nobom:
                return get(Encoding.BigEndianUnicode, false, () => new UnicodeEncoding(true, false));
            case EncodingName.utf_32:
                return get(Encoding.UTF32, true, () => new UTF32Encoding(false, true));
            case EncodingName.utf_32_nobom:
                return get(Encoding.UTF32, false, () => new UTF32Encoding(false, false));
            case EncodingName.utf_32be:
                return get(null, true, () => new UTF32Encoding(true, true));
            case EncodingName.utf_32be_nobom:
                return get(null, false, () => new UTF32Encoding(true, false));
            default:
                throw Contract.AssertFailure($"Unexpected enum value: {name}");
        }
    }

    public static SourceEncodingInfo ConvertFromJson(EncodingName jsonFormat)
    {
        return FromEncoding(GetEncoding(jsonFormat));
    }

    public EncodingName ConvertToJson()
    {
        return EncodingName;
    }
}

public enum EncodingName
{
    utf_8_nobom = 0,
    utf_8,
    us_ascii_nobom,
    us_ascii,
    utf_16_nobom,
    utf_16,
    utf_16be_nobom,
    utf_16be,
    utf_32_nobom,
    utf_32,
    utf_32be_nobom,
    utf_32be,
}