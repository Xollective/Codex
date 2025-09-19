using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Codex.Utilities.Serialization;
using Span = Codex.Utilities.Extent;

namespace Codex.Utilities
{
    public static class StringEx
    {
        public static string JoinNonEmpty(string separator, params string[] values)
        {
            return string.Join(separator, values.WhereNotNullOrEmpty());
        }
    }

    public ref struct SpanMatch(ReadOnlySpan<char> text, ValueMatch match, bool success = true)
    {
        public bool Success { get; } = success;
        public ValueMatch Match { get; } = match;
        public ReadOnlySpan<char> Text { get; } = text;

        public ReadOnlySpan<char> ValueSpan => Text.Slice(Match.Index, Match.Length);

        public int Length => Match.Length;
    }

    public static class StringExtensions
    {
        public static string StringJoin(this IEnumerable<string> strings, string separator = "")
        {
            return string.Join(separator, strings);
        }

        public static SpanMatch Match(this Regex r, ReadOnlySpan<char> input)
        {
            foreach (var match in r.EnumerateMatches(input))
            {
                return new SpanMatch(input, match);
            }


            return default;
        }

        public static bool Success(this ValueMatch m) => m.Length > 0;

        //public static ValueMatch Match(this Regex r, Span<char> input)
        //{
        //    return Match(r, input.AsReadOnly());
        //}

        public static void WriteLineAndClear(this TextWriter writer, StringBuilder b)
        {
            writer.WriteLine(b);
            b.Clear();
            writer.Flush();
        }

        public static bool HasExtension(this string path, params string[] extensions)
        {
            var actualExtension = Path.GetExtension(path).TrimStart('.');
            return actualExtension != null
                && extensions.Any(e => actualExtension.Equals(e, StringComparison.OrdinalIgnoreCase));
        }

        public static ReadOnlySpan<char> SpanTrim(this string s) => s.AsSpan().Trim();

        public static bool IsNonEmpty(this string s)
        {
            return !string.IsNullOrEmpty(s);
        }

        public static string AsNotEmptyOrNull(this string s)
        {
            return !string.IsNullOrEmpty(s) ? s : null;
        }

        public static char? CharAt(this string s, int index) => (uint)index >= (uint)s.Length ? null : s[index];

        public static bool ContainsIgnoreCase(this string s, string value, string valueSuffix = null)
        {
            bool result = ContainsIgnoreCase(s, value, out var index);
            if (result && !string.IsNullOrEmpty(valueSuffix))
            {
                result = s.AsSpan(index + value.Length).StartsWith(valueSuffix, StringComparison.OrdinalIgnoreCase);
            }

            return result;
        }

        public static bool ContainsIgnoreCase(this string s, string value, out int index)
        {
            return (index = s.IndexOf(value, StringComparison.OrdinalIgnoreCase)) > -1;
        }

        public static bool EndsWithIgnoreCase(this string s, string value)
        {
            return s.EndsWith(value, StringComparison.OrdinalIgnoreCase);
        }

        public static bool StartsWithIgnoreCase(this string s, string value)
        {
            return s.StartsWith(value, StringComparison.OrdinalIgnoreCase);
        }

        public static void Replace(this Span<char> span, char source, char target)
        {
            for (int i = 0; i < span.Length; i++)
            {
                ref char ch = ref span[i];
                if (ch == source) ch = target;
            }
        }

        public static ReadOnlySpan<char> SubstringBeforeFirstIndexOfAny(this ReadOnlySpan<char> s, ReadOnlySpan<char> chars)
        {
            var lastIndex = s.IndexOfAny(chars);
            if (lastIndex > 0)
            {
                return s.Slice(0, lastIndex);
            }

            return s;
        }

        public static ReadOnlySpan<char> SubstringBeforeLastIndexOfAny(this ReadOnlySpan<char> s, ReadOnlySpan<char> chars)
        {
            var lastIndex = s.LastIndexOfAny(chars);
            if (lastIndex > 0)
            {
                return s.Slice(0, lastIndex);
            }

            return s;
        }

        public static ReadOnlySpan<char> SubstringFromFirstIndexOfAny(this ReadOnlySpan<char> s, ReadOnlySpan<char> chars)
        {
            var lastIndex = s.IndexOfAny(chars);
            if (lastIndex > 0)
            {
                return s.Slice(lastIndex);
            }

            return s;
        }

        public static bool SplitAtFirstIndexOf(
            this ReadOnlySpan<char> s,
            ReadOnlySpan<char> splitChars,
            out ReadOnlySpan<char> prefix,
            out ReadOnlySpan<char> suffix)
        {
            var index = s.IndexOf(splitChars);
            if (Extent.IsValidIndex(index, s.Length))
            {
                prefix = s.Slice(0, index);
                suffix = s.Slice(index + 1);
                return true;
            }
            else
            {
                prefix = suffix = default;
                return false;
            }
        }

        public static ReadOnlySpan<char> SubstringAfterLastIndexOfAny(this ReadOnlySpan<char> s, ReadOnlySpan<char> chars, bool requireMatch = false)
        {
            var lastIndex = s.LastIndexOfAny(chars);
            if (lastIndex > 0)
            {
                return s.Slice(lastIndex + 1);
            }
            else if (lastIndex < 0 && requireMatch)
            {
                return default;
            }

            return s;
        }

        public static string TrimStartIgnoreCase(this string s, string value)
        {
            if (s.StartsWith(value, StringComparison.OrdinalIgnoreCase))
            {
                return s.Substring(value.Length);
            }

            return s;
        }

        public static string TrimStart(this string s, string value)
        {
            if (s.StartsWith(value, StringComparison.Ordinal))
            {
                return s.Substring(value.Length);
            }

            return s;
        }

        public static string TrimEndIgnoreCase(this string s, string value)
        {
            if (s.EndsWith(value, StringComparison.OrdinalIgnoreCase))
            {
                return s.Substring(0, s.Length - value.Length);
            }

            return s;
        }

        public static bool IsArgument(this string argument, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(argumentName))
            {
                return false;
            }

            if (argument.StartsWith("/") || argument.StartsWith("-"))
            {
                argument = argument.Substring(1);
            }

            return string.Equals(argument, argumentName);
        }

        public const string LineBreakCharacters = "\r\n";

        public static bool IsLineBreakChar(this char c)
        {
            return c == '\r' || c == '\n';
        }

        public static string EscapeLineBreaks(this string text)
        {
            if (text == "\r\n")
            {
                return @"\r\n";
            }
            else if (text == "\n")
            {
                return @"\n";
            }

            return text
                .Replace("\r\n", @"\r\n")
                .Replace("\n", @"\n")
                .Replace("\r", @"\r")
                .Replace("\u0085", @"\u0085")
                .Replace("\u2028", @"\u2028")
                .Replace("\u2029", @"\u2029");
        }

        public static string GetFirstLine(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            int cr = text.IndexOf('\r');
            int lf = text.IndexOf('\n');
            if (cr > 0)
            {
                if (lf > 0 && lf < cr)
                {
                    cr = lf;
                }

                text = text.Substring(0, cr);
            }
            else if (lf > 0)
            {
                text = text.Substring(0, lf);
            }

            return text;
        }

        public static string ReplaceIgnoreCase(this string input, string oldValue, string newValue)
        {
            return input.Replace(oldValue, newValue, StringComparison.OrdinalIgnoreCase);
        }

        public static string Truncate(this string input, int maxLength)
        {
            if (input.Length > maxLength)
            {
                return input.Substring(0, maxLength);
            }

            return input;
        }

        public static string ToUpper(this Guid guid)
        {
            return guid.ToString("B").ToUpperInvariant();
        }

        public static void CollectLineSpans(this string text, ICollection<Span> spans, bool includeLineBreakInSpan = true)
        {
            if (spans == null)
            {
                throw new ArgumentNullException(nameof(spans));
            }

            foreach (var span in text.EnumerateLineSpans(includeLineBreakInSpan))
            {
                spans.Add(span);
            }
        }

        public static IEnumerable<(Extent TrimmedLine, Extent FullLine)> GetTrimmedLineSpans(this string content)
        {
            foreach ((var line, int index) in content.EnumerateLineSpans(includeLineBreakInSpan: true).WithIndices())
            {
                var trimmedLine = line;
                CharString cs = content.GetCharString(line);

                cs = cs.TrimEnd();
                trimmedLine = trimmedLine.TruncateEnd(cs.Length);

                if (index != 0)
                {
                    // Don't trim leading whitespace on first line since it typically contains
                    // file reference/definition at position zero
                    cs = cs.TrimStart();
                    trimmedLine = trimmedLine.TruncateStart(cs.Length);
                }

                var result = (trimmedLine, line);
                yield return result;
            }
        }

        public static IEnumerable<Span> EnumerateLineSpans(this string text, bool includeLineBreakInSpan = true)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (text.Length == 0)
            {
                yield return new Span(0, 0);
                yield break;
            }

            int currentPosition = 0;
            int currentLineLength = 0;
            bool previousWasCarriageReturn = false;

            foreach (var ch in text)
            {
                if (ch == '\r')
                {
                    if (previousWasCarriageReturn)
                    {
                        int lineLengthIncludingLineBreak = currentLineLength;
                        if (!includeLineBreakInSpan)
                        {
                            currentLineLength--;
                        }

                        yield return new Span(currentPosition, currentLineLength);

                        currentPosition += lineLengthIncludingLineBreak;
                        currentLineLength = 1;
                    }
                    else
                    {
                        currentLineLength++;
                        previousWasCarriageReturn = true;
                    }
                }
                else if (ch == '\n')
                {
                    var lineLength = currentLineLength;
                    if (previousWasCarriageReturn)
                    {
                        lineLength--;
                    }

                    currentLineLength++;
                    previousWasCarriageReturn = false;
                    if (includeLineBreakInSpan)
                    {
                        lineLength = currentLineLength;
                    }

                    yield return new Span(currentPosition, lineLength);
                    currentPosition += currentLineLength;
                    currentLineLength = 0;
                }
                else
                {
                    if (previousWasCarriageReturn)
                    {
                        var lineLength = currentLineLength;
                        if (!includeLineBreakInSpan)
                        {
                            lineLength--;
                        }

                        yield return new Span(currentPosition, lineLength);
                        currentPosition += currentLineLength;
                        currentLineLength = 0;
                    }

                    currentLineLength++;
                    previousWasCarriageReturn = false;
                }
            }

            var finalLength = currentLineLength;
            if (previousWasCarriageReturn && !includeLineBreakInSpan)
            {
                finalLength--;
            }

            yield return new Span(currentPosition, finalLength);

            if (previousWasCarriageReturn)
            {
                yield return new Span(currentPosition, 0);
            }
        }

        private static readonly IReadOnlyList<Span> EmptySpanList = new Span[] { default(Span) };

        public static IReadOnlyList<Span> GetLineSpans(this string text, bool includeLineBreakInSpan)
        {
            if (string.IsNullOrEmpty(text))
            {
                return EmptySpanList;
            }

            var result = new List<Span>();
            text.CollectLineSpans(result, includeLineBreakInSpan);
            return result.ToArray();
        }

        public static IReadOnlyList<string> GetLines(this string text, IReadOnlyList<Span> lineSpans, bool includeLineBreak = false)
        {
            if (text == null)
            {
                return Array.Empty<string>();
            }

            var lines = new string[lineSpans.Count];
            int endExclusive = text.Length;
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                var lineSpan = lineSpans[i];
                if (includeLineBreak)
                {
                    lineSpan = Span.FromBounds(lineSpan.Start, endExclusive);
                    endExclusive = lineSpan.Start;
                }

                lines[i] = text.Substring(lineSpan.Start, lineSpan.Length);
            }

            return lines;
        }

        public static IEnumerable<(int index, Span span)> GetLineBreaksFromEnd(this string text, IReadOnlyList<Span> lineSpansWithoutBreaks)
        {
            int endExclusive = text.Length;
            for (int i = lineSpansWithoutBreaks.Count - 1; i >= 0; i--)
            {
                var lineSpan = lineSpansWithoutBreaks[i];
                var breakSpan = Span.FromBounds(lineSpan.EndExclusive, endExclusive);
                if (breakSpan.Length != 0)
                {
                    yield return (i, breakSpan);
                }
                endExclusive = lineSpan.Start;
            }
        }

        public static IReadOnlyList<string> GetLines(this string text, bool includeLineBreak = false)
        {
            if (text == null)
            {
                return Array.Empty<string>();
            }

            return GetLineSpans(text, includeLineBreakInSpan: includeLineBreak)
                .Select(span => text.Substring(span.Start, span.Length))
                .ToArray();
        }

        public static IEnumerable<string> WhereNotNullOrEmpty(this IEnumerable<string> strings)
        {
            return strings.Where(s => !string.IsNullOrEmpty(s));
        }
    }
}
