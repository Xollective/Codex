using System.Text;
using Codex.ObjectModel.CompilerServices;
using Codex.Sdk;
using Codex.Storage.BlockLevel;
using Codex.Utilities;
using Codex.Web.Mvc.Rendering;
using CommunityToolkit.HighPerformance;
using static Codex.Utilities.CollectionUtilities;

namespace Codex.View
{
    [Flags]
    public enum SourceFileViewFlags
    {
        None,
        Locals = 1,
        RefCount = 1 << 1,
        RefId = 1 << 2,
        RefProject = 1 << 3,
        Classifications = 1 << 4,
        RefInfo = RefId | RefCount | RefProject
    }

    public record SourceFileViewModel(IBoundSourceFile SourceFile) : SourceFileModel(SourceFile)
    {
        public IReadOnlyList<TextSpanSearchResultViewModel> References =>
            SourceFile.References.SelectList(s => new TextSpanSearchResultViewModel(new ReferenceSearchResult()
            {
                File = SourceFile.SourceFile.Info,
                ReferenceSpan = s
            }));

        public BindableValue<ILineSpan> TargetSpan { get; } = new BindableValue<ILineSpan>();

        public ViewModelAddress? OpenFileLink { get; set; }

        public List<HtmlElementInfo> ReferenceHtml { get; set; }
        public List<BaseElementInfo> AllHtml { get; set; }

        public ListSegment<HtmlElementInfo> TryFind(int charPosition)
        {
            string ranges = string.Join(", ", ReferenceHtml.Select(i => $"({i.Span.Start}, {i.Span.End()})"));
            return ReferenceHtml.GetRange<HtmlElementInfo, int>(charPosition, (pos, r) => -r.Range.CompareTo(pos), (pos, r) => -r.Range.CompareTo(pos));
        }

        public ListSegment<HtmlElementInfo> TryFind(string searchString)
        {
            int offset = searchString.IndexOf('*');
            if (offset < 0)
            {
                offset = 0;
            }
            else
            {
                searchString = searchString.Replace("*", "");
            }

            var pos = SourceFile.SourceFile.Content.IndexOf(searchString, StringComparison.OrdinalIgnoreCase);
            return TryFind(pos + offset);
        }

        public StringBuilder GetXmlDump(SourceFileViewFlags flags, IEnumerable<SourceSpan> overrideSpans = null)
        {
            var renderer = new XmlDumpSourceRenderer(new(), flags);
            Append(renderer, overrideSpans);
            return renderer.Builder;
        }

        public void Append(ISourceFileRenderer renderer, IEnumerable<SourceSpan> overrideSpans = null)
        {
            int textIndex = 0;

            Span<char> buffer = stackalloc char[256];
            //string sourceText = SourceFile.SourceFile.Content;
            using var reader = SourceFile.SourceFile.ContentSource.GetSourceReader();

            var rawHandler = reader.CreateHandler(
                renderer, 
                (chars, renderer) => renderer.AppendRaw(chars),
                buffer);

            renderer.Start();

            foreach (var span in GetProcessedSpans(overrideSpans))
            {
                if (span.Start > textIndex)
                {
                    var diff = span.Start - textIndex;

                    //Span is ahead of our current index, just write the normal text between the two to the buffer
                    rawHandler.ReadRequired(diff);
                    textIndex = span.Start;
                }

                if (span.Length == 0) continue;

                renderer.Append(span, reader.ReadRequired(span.Length, buffer));

                textIndex += span.Length;
            }

            // Append any leftover text
            rawHandler.ReadRemaining();

            renderer.Finish();
        }

        private record XmlDumpSourceRenderer(StringBuilder Builder, SourceFileViewFlags Flags) : ISourceFileRenderer
        {
            private const char LessThanAlt = (char)0x02C2;
            private const char GreaterThanAlt = (char)0x02C3;

            public void Start()
            {
                Builder.AppendLine("<SourceFile>");
            }

            public void Append(SourceSpan span, StringSpan text)
            {
                var flags = Flags;
                if (!(span.Classification?.LocalGroupId > 0))
                {
                    flags &= ~SourceFileViewFlags.Locals;
                }

                if (!(span.Classification?.Classification.Value > 0))
                {
                    flags &= ~SourceFileViewFlags.Classifications;
                }

                if (!(span.SpanReferences?.Count > 0))
                {
                    flags &= ~SourceFileViewFlags.RefCount;
                    flags &= ~SourceFileViewFlags.RefId;
                    flags &= ~SourceFileViewFlags.RefProject;
                }

                if (flags == SourceFileViewFlags.None)
                {
                    AppendRaw(text);
                    return;
                }

                Builder.Append("<s");
                if (flags.HasFlag(SourceFileViewFlags.Classifications))
                {
                    Builder.Append($" c=\"{span.Classification.Classification.IntegralValue}\"");
                }

                if (flags.HasFlag(SourceFileViewFlags.Locals))
                {
                    Builder.Append($" lid=\"{span.Classification.LocalGroupId}\"");
                }

                if (flags.HasFlag(SourceFileViewFlags.RefCount))
                {
                    Builder.Append($" rc=\"{span.SpanReferences?.Count}\"");
                }

                if (flags.HasFlag(SourceFileViewFlags.RefId))
                {
                    Builder.Append($" rid=\"{span.Reference.Reference.Id.Value}\"");
                }

                if (flags.HasFlag(SourceFileViewFlags.RefProject))
                {
                    Builder.Append($" rc=\"{span.Reference.Reference.ProjectId}\"");
                }

                Builder.Append(">");
                AppendRaw(text);
                Builder.Append("</s>");
            }

            public void AppendRaw(StringSpan text)
            {
                var start = Builder.Length;
                Builder.Append(text.Chars);
                Builder.Replace('<', LessThanAlt, start, text.Length);
                Builder.Replace('>', GreaterThanAlt, start, text.Length);
            }

            private static string ReplaceText(string text)
            {
                text = text.Replace('<', LessThanAlt).Replace('>', GreaterThanAlt);
                return text;
            }

            public void Finish()
            {
                Builder.AppendLine();
                Builder.Append("</SourceFile>");
            }
        }
    }

    public interface ISourceFileRenderer
    {
        void Start() { }

        void AppendRaw(StringSpan text);

        void Append(SourceSpan span, StringSpan text);

        void Finish() { }
    }

    public ref struct StringSpan(ReadOnlySpan<char> chars = default, string stringValue = null)
    {
        public int Length => Chars.Length;

        public readonly ReadOnlySpan<char> Chars = stringValue != null ? stringValue : chars;
        private string _stringValue = stringValue;

        public string StringValue => _stringValue ??= Chars.ToString();

        public static implicit operator ReadOnlySpan<char>(StringSpan value) => value.Chars;
        public static implicit operator StringSpan(string value) => new(stringValue: value);
        public static implicit operator StringSpan(ReadOnlySpan<char> value) => new(chars: value);

        public override string ToString()
        {
            return _stringValue ??= Chars.ToString();
        }
    }
}
