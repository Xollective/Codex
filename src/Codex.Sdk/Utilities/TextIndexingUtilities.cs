using System.Text;
using Codex.ObjectModel;
using Codex.Sdk.Utilities;

namespace Codex.Utilities
{
    public static class TextIndexingUtilities
    {
        public static ICollection<T> ToCollection<T>(this T value) where T : class
        {
            if (value == default(T))
            {
                // Use singleton array after switching to .NET 4.6
                return new T[] { };
            }

            return new T[] { value };
        }

        public static List<ReferenceSpan> GetReferences(this IEnumerable<SourceSpan> spans)
        {
            return spans.Where(s => s.SpanReferences.HasValue).SelectMany(s => s.SpanReferences.Value).Cast<ReferenceSpan>().ToList();
        }

        public static IEnumerable<SourceSpan> Rejoin(this IEnumerable<SourceSpan> spans)
        {
            Extent? lastRange = null;
            foreach ((var span, var nextSpan, int index, bool isLast) in spans.WithNexts())
            {
                if (!isLast
                    && span.Classification != null
                    && span.Range.EndExclusive == nextSpan?.Start
                    && span.Classification.Classification == nextSpan?.Classification?.Classification
                    && span.SpanReferences == null && nextSpan?.SpanReferences == null)
                {
                    lastRange ??= span.Range;
                }
                else if (lastRange != null)
                {
                    yield return span with { Range = span.Range.Union(lastRange.Value) };
                    lastRange = null;
                }
                else
                {
                    yield return span;
                }
            }
        }

        public static void ToChunks(this ISourceFile sourceFile, out ChunkedSourceFile chunkFile, out IReadOnlyList<TextChunkSearchModel> chunks, Action<TextChunkSearchModel> populate)
        {
            var contentMemory = sourceFile.Content.AsMemory();
            var lines = sourceFile.Content.GetLineSpans(includeLineBreakInSpan: true)
                .SelectList(r => new ReadOnlySegment<char>(contentMemory, r));

            var encoding = sourceFile.Info.EncodingInfo.Encoding;

            IEnumerable<int> getLineOffsets()
            {
                int offset = sourceFile.Info.EncodingInfo.PreambleLength;
                yield return offset;
                foreach (var line in lines)
                {
                    offset += encoding.GetByteCount(line.Span);
                    yield return offset;
                }
            }

            var lineChunks = IndexingUtilities.GetTextIndexingChunks(lines);

            chunkFile = new ChunkedSourceFile(sourceFile);

            var chunkList = new List<TextChunkSearchModel>();
            chunks = chunkList;

            int startLineNumber = 0;
            foreach (var lineChunk in lineChunks)
            {
                var chunkSearchModel = new TextChunkSearchModel();
                var content = lineChunk[0].WithEnd(lineChunk[^1].Range.EndExclusive);
                chunkSearchModel.Content = content.AsMemory().ToString();

                // TODO: Implement chunkSearchModel.PopulateContentIdAndSize(); at this layer
                populate?.Invoke(chunkSearchModel);
                chunkList.Add(chunkSearchModel);
                chunkFile.Chunks.Add(new ChunkReference()
                {
                    Id = chunkSearchModel.StableId,
                    StartLineNumber = startLineNumber
                });

                startLineNumber += lineChunk.Count;
            }
        }
    }
}