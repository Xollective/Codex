using System.Globalization;
using Codex.Utilities;
using Codex.Web.Common.Properties;
using static Lucene.Net.Search.FieldCache;

namespace Codex.Web.Common;

public record struct ClassificationFormat(uint LightColor, uint DarkColor)
{
    public static Dictionary<StringEnum<ClassificationName>, ClassificationFormat> FormatMap { get; }
        = GetFormatMap();

    private static Dictionary<StringEnum<ClassificationName>, ClassificationFormat> GetFormatMap()
    {
        var formatMap = new Dictionary<StringEnum<ClassificationName>, ClassificationFormat>();
        void parse(byte[] bytes, bool light)
        {
            var map = JsonSerializationUtilities.DeserializeEntity<Dictionary<StringEnum<ClassificationName>, string>>(bytes);

            foreach (var (key, value) in map)
            {
                var span = value.AsSpan().Trim('#');
                var color = uint.Parse(span, NumberStyles.HexNumber);
                if (span.Length == 6)
                {
                    color |= 0xFF000000;
                }
                
                if (light)
                {
                    formatMap[key] = new ClassificationFormat(color, color);
                }
                else if (formatMap.TryGetValue(key, out var format))
                {
                    formatMap[key] = format with { DarkColor = color };
                }
            }
        }

        parse(Resources.LightJson, light: true);
        parse(Resources.DarkJson, light: false);

        return formatMap;
    }
}
