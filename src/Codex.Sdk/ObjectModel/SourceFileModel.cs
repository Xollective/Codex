using Codex.Sdk.Utilities;
using Codex.Storage.BlockLevel;
using static Codex.Utilities.CollectionUtilities;
using Extent = Codex.Utilities.Extent;

namespace Codex.ObjectModel
{
    public record SourceFileModel(IBoundSourceFile SourceFile, bool IncludeOutline = false)
    {
        public bool RemapLocalIds { get; set; } = true;

        public bool AllowEmpty { get; set; } = true;

        public int? LineNumber { get; set; }

        public IReadOnlyList<ClassifiedTextSpan> Classifications =>
            SourceFile.Classifications.SelectList(s => new ClassifiedTextSpan(s, SourceFile.SourceFile.Content));

        private static IComparer<ISpan> SpanLengthComparer { get; } = new ComparerBuilder<ISpan>()
            .CompareByAfter(s => s.Length);

        public IEnumerable<SourceSpan> GetProcessedSpans(IEnumerable<SourceSpan> overrideSpans = null, bool trim = false)
        {
            var localTracker = new ScopeTracker();
            var spans = overrideSpans;
            spans ??= SourceFile.As(BoundSourceFile.Type)?.SourceSpans;
            spans ??= GetSpans();
            var remap = SourceFile.Flags.HasFlag(BoundSourceFlags.RemapLocalIds) && RemapLocalIds;

            if (trim)
            {
                var trimmedLines = SourceFile.SourceFile.Content.GetTrimmedLineSpans().Select(t => t.TrimmedLine);

                spans = IndexingUtilities.GetLineSpans(spans, trimmedLines, allowEmpty: true).Select(
                    ls => ls.Value with { Range = ls.Intersect });
            }

            foreach (var span in spans)
            {
                if (remap && span.Classification?.LocalGroupId > 0)
                {
                    var cspan = new ClassificationSpan(span.Classification);
                    cspan.LocalGroupId = localTracker.GetLocalId(span.Classification);
                    yield return span with { Classification = cspan };
                }
                else
                {
                    yield return span;
                }
            }
        }

        private IEnumerable<SourceSpan> GetSpans()
        {
            var classifications = SourceFile.Classifications;
            var references = GetBestReferences();

            (IClassificationSpan classification, UnifiedReferenceSpan reference, MergeMode mode) state = default;
            int cursor = 0;

            ISpan hint = default;

            var lazyContent = new Lazy<string>(() => SourceFile.SourceFile.Content);

            foreach ((var item, var next, int index, bool isLast) in DistinctMergeSorted(classifications, references, GetCompare, GetCompare).WithNexts())
            {
                var greatestOnly = item.GreatestOnly(SpanLengthComparer);
                var (classification, reference, mode) = item;
                var maxPosition = next?.Either(hint).Start ?? greatestOnly.Either(hint).End();

                bool tryGetSpanAndMoveCursor(int position, out SourceSpan span, bool allowEmpty = false)
                {
                    bool hasSpan = false;
                    span = default;
                    if (position >= maxPosition)
                    {
                        position = maxPosition;
                    }

                    if (allowEmpty ? position >= cursor : position > cursor)
                    {
                        hasSpan = true;
                        Contract.Assert(state.classification != null || state.reference != null, "Classification and reference cannot be null");
                        span = new SourceSpan(Extent.FromBounds(cursor, position), state.classification, state.reference?.Segment, lazyContent);
                    }

                    cursor = position;
                    return hasSpan;
                }

                var start = item.Either(hint).Start;
                cursor = Math.Max(cursor, start);

                state = item;
                if (tryGetSpanAndMoveCursor(item.Least(SpanLengthComparer).End(), out var sourceSpan, allowEmpty: AllowEmpty))
                {
                    yield return sourceSpan;
                }

                state = greatestOnly;
                if (tryGetSpanAndMoveCursor(state.Either(hint).End(), out sourceSpan, allowEmpty: false))
                {
                    yield return sourceSpan;
                }
            }
        }

        private static (int start, int isNonEmpty) GetCompare(ISpan span) => (span.Start, span.Length == 0 ? 0 : int.MaxValue);

        public record UnifiedReferenceSpan(ListSegment<IReferenceSpan> Segment) : ISpan
        {
            public int Start => BestReference.Start;

            public int Length => BestReference.Length;

            public IReferenceSpan BestReference { get; } = GetBestReference(Segment);

            public IReferenceSpan GetBestReference_ForTest() => GetBestReference(Segment);
        }

        public IEnumerable<UnifiedReferenceSpan> GetBestReferences()
        {
            var groupedReferences = SourceFile.References.SortedGroupBy(GetCompare);
            var groupedOutlines = SourceFile.References.SortedGroupBy(GetCompare);

            foreach (var referenceGroup in SourceFile.References.SortedGroupBy(r => r.Start))
            {
                yield return new(referenceGroup.Items);
            }
        }

        public static IReferenceSpan GetBestReference(ListSegment<IReferenceSpan>? references)
        {
            if (references == null) return null;

            IReferenceSpan bestReference = null;
            foreach (var reference in references)
            {
                if (bestReference == null) bestReference = reference;
                else bestReference = GetBestReference(bestReference, reference);
            }

            return bestReference;
        }

        public static IReferenceSpan GetBestReference(IReferenceSpan current, IReferenceSpan candidate)
        {
            return BestReferenceComparer.GetSortedFirst(current, candidate);
        }

        private static IComparer<IReferenceSpan> BestReferenceComparer = new ComparerBuilder<IReferenceSpan>()
            // Prefer references which are not implicitly declared
            .CompareByAfter(r => r.IsImplicitlyDeclared ? 1 : 0)
            .CompareByAfter(r => GetReferenceKindRank(r.Reference))
            .CompareByAfter(r => GetSymbolKindRank(r.Reference.Kind));

        private static int GetReferenceKindRank(IReferenceSymbol reference)
        {
            var kind = reference.ReferenceKind;

            switch (kind)
            {
                // Prefer definitions
                case ReferenceKind.Definition:
                case ReferenceKind.Constructor:
                    return -5;

                // Spurn instantations since constructor reference should
                // be preferred
                case ReferenceKind.Instantiation:

                // Spurn explicit casts since reference to operator should
                // be preferred
                case ReferenceKind.ExplicitCast:
                    return 1;
            }

            return 0;
        }

        private static int GetSymbolKindRank(StringEnum<SymbolKinds> kind)
        {
            if (kind.Value is SymbolKinds kindValue)
            {
                switch (kindValue)
                {
                    // Prefer constructors. This is needed since spans have reference to constructor and
                    // type symbol (instantiation).
                    case SymbolKinds.Constructor:
                        return -1;
                }
            }

            return 0;
        }
    }

    public record struct SourceSpan(Extent Range, IClassificationSpan Classification, ListSegment<IReferenceSpan>? SpanReferences, Lazy<string>? SourceContent = default) : ISpan
    {
        public int Start => Range.Start;

        public int Length => Range.Length;

        public IReferenceSpan Reference => SourceFileModel.GetBestReference(SpanReferences);

        public string? Segment => SourceContent?.Value?.Substring(Start, Length);
    }

    public record struct ClassifiedTextSpan(IClassificationSpan Span, string Content)
    {
        public override string ToString()
        {
            return $"'{Content.Substring(Span.Start, Span.Length)}' ({Span.Start}, {Span.Length}) ({Span.Classification}) [{Span.LocalGroupId}]";
        }
    }
}
