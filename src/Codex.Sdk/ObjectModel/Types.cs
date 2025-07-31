using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.ContractsLight;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codex.ObjectModel;
using Codex.ObjectModel.Attributes;
using Codex.Sdk.Search;
using Codex.Utilities.Serialization;
using static Codex.Utilities.SerializationUtilities;

namespace Codex.ObjectModel.Implementation
{
    /// <summary>
    ///  Allows defining extension data during analysis
    /// </summary>
    public class ExtensionData
    {
    }

    public static class TypeExtensions
    {
        public static RepoAccess? GetEffectiveAccess(this IUserSettings settings, DateTime? utcNow = null)
        {
            utcNow ??= DateTime.UtcNow;
            return settings?.ExpirationUtc == default(DateTime) || settings?.ExpirationUtc > utcNow
                ? settings.Access
                : null;
        }

        public static string GetFindAllReferencesDisplayName(this IDefinitionSymbol symbol)
        {
            return symbol.ShortName;
        }

        public static DefinitionSymbol? AsDefinition(this ICodeSymbol symbol)
        {
            return (symbol is DefinitionSymbol def && !string.IsNullOrEmpty(def.DisplayName ?? def.ShortName)) ? def : null;
        }

        public static bool IsTopLevel(this IDefinitionSymbol symbol) => symbol.Kind.ValueOrDefault().IsTypeKind();

        public static bool SymbolEquals(this ICodeSymbol symbol, ICodeSymbol other)
        {
            return string.Equals(symbol.ProjectId, other.ProjectId, StringComparison.Ordinal) && string.Equals(symbol.Id.Value, other.Id.Value, StringComparison.Ordinal);
        }

        public static IEnumerable<IReferenceSymbol> GetSymbols(this IEnumerable<IReferenceSpan> spans)
        {
            if (spans is IReferenceListModel listModel)
            {
                return listModel.SharedValues;
            }

            return spans.Select(rs => rs.Reference);
        }

        public static IEnumerable<RichText> GetRichTextSpans(this ITextLineSpan referringSpan)
        {
            var lineSpanText = referringSpan.LineSpanText;
            if (lineSpanText != null)
            {
                yield return new RichText(lineSpanText.AsMemory(0, referringSpan.LineSpanStart));
                yield return new RichText(lineSpanText.AsMemory(referringSpan.LineSpanStart, referringSpan.Length), Highlighted: true);
                yield return new RichText(lineSpanText.AsMemory(referringSpan.LineSpanStart + referringSpan.Length));
            }
        }

        public static ReferenceSymbol ToReferenceSymbol(this ISharedReferenceInfo info, ICodeSymbol symbol)
        {
            return new ReferenceSymbol(symbol)
            {
                ExcludeFromSearch = info.ExcludeFromSearch,
                ReferenceKind = info.ReferenceKind,
            };
        }

        public static ReferenceSpan ToReferenceSpan(this ISharedReferenceInfoSpan span, ICodeSymbol symbol)
        {
            return new ReferenceSpan(span)
            {
                Reference = span.Info.ToReferenceSymbol(symbol),
                RelatedDefinition = span.Info.RelatedDefinition
            };
        }

        public static Extent AsRange(this ISpan span) => (span.Start, span.End());
    }

    public partial class SearchEntity
    {
        public int DocId { get; set; }
    }

    public partial class RepositoryStoreInfo
    {
        public RepositoryStoreInfo(Repository repository, Commit commit, Branch branch)
        {
            Repository = repository;
            Commit = commit;
            Branch = branch;
        }

        public void Deconstruct(out Repository repository, out Commit commit, out Branch branch)
        {
            (repository, commit, branch) = (Repository, Commit, Branch);
        }
    }

    public partial class CodeSymbol
    {
        public static readonly IUnifiedComparer<RelatedDefinition> RelatedDefinitionComparer = new ComparerBuilder<RelatedDefinition>()
            .CompareByAfter(r => r.Symbol, SymbolComparer)
            .CompareByAfter(r => r.ReferenceKind);

        public static IEqualityComparer<ICodeSymbol> SymbolEqualityComparer => SymbolComparer;

        public static IUnifiedComparer<ICodeSymbol> SymbolComparer { get; } = new ComparerBuilder<ICodeSymbol>()
            .CompareByAfter(s => s.ProjectId, ProjectIdComparer)
            .CompareByAfter(s => s.Id.Value)
            ;

        public static StringComparer ProjectIdComparer { get; } = StringComparer.Ordinal;

        public static ComparerBuilder<ISpanWithSymbol> SymbolSpanSorter = new ComparerBuilder<ISpanWithSymbol>()
            .CompareByAfter(c => c, Span.StartAndLengthComparer)
            .CompareByAfter(c => (int)c.Symbol.ReferenceKind)
            .CompareByAfter(c => c.Symbol.ProjectId, ProjectIdComparer)
            .CompareByAfter(c => c.Symbol.Id.Value);

        /// <summary>
        /// Extension data used during analysis/search
        /// TODO: Why is this needed?
        /// </summary>
        public ExtensionData ExtData { get; set; }

        protected bool Equals(CodeSymbol other)
        {
            return this.SymbolEquals(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CodeSymbol)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ProjectId?.GetHashCode() ?? 0) * 397) ^ (Id.Value?.GetHashCode() ?? 0);
            }
        }

        public override string ToString()
        {
            return Id.Value;
        }
    }

    partial class SharedReferenceInfoSpan
    {
        public static SharedReferenceInfoSpan From(IReferenceSpan span)
        {
            return new SharedReferenceInfoSpan(span)
            {
                Info = SharedReferenceInfo.From(span)
            };
        }
    }

    //partial class SearchEntity : ISearchEntity
    //{
    //    SearchType ITypedSearchEntity.GetSearchType() => throw new NotSupportedException();
    //}

    partial class SharedReferenceInfo : IShouldSerializeProperty<ISharedReferenceInfo>
    {
        protected override void Initialize()
        {
            ReferenceKind = ObjectModel.ReferenceKind.Reference;
            base.Initialize();
        }

        public static SharedReferenceInfo From(IReferenceSpan span)
        {
            return new()
            {
                ReferenceKind = span.Reference.ReferenceKind,
                RelatedDefinition = span.RelatedDefinition,
                ExcludeFromSearch = span.Reference.ExcludeFromSearch
            };
        }

        public static bool ShouldSerializeProperty(ISharedReferenceInfo obj, string propertyName)
        {
            switch (propertyName)
            {
                case nameof(ReferenceKind):
                    return obj.ReferenceKind != ObjectModel.ReferenceKind.Reference;
            }

            return true;
        }
    }

    partial class ReferenceSymbol : IShouldSerializeProperty<IReferenceSymbol>
    {
        protected override void Initialize()
        {
            ReferenceKind = ObjectModel.ReferenceKind.Reference;
            base.Initialize();
        }

        public override string ToString()
        {
            return ReferenceKind + " " + base.ToString();
        }

        protected virtual ReferenceKind CoerceReferenceKind(ReferenceKind value)
        {
            return value;
        }

        public static bool ShouldSerializeProperty(IReferenceSymbol obj, string propertyName)
        {
            switch (propertyName)
            {
                case nameof(ReferenceKind):
                    return obj.ReferenceKind != ObjectModel.ReferenceKind.Reference;
            }

            return true;
        }
    }

    partial class ReferenceSearchResult
    {
        public override string ToString()
        {
            return ReferenceSpan?.ToString();
        }
    }

    partial class ReferencedProject
    {
        public int CoerceDefinitionCount(int? value)
        {
            return value ?? Definitions.Count;
        }
    }

    partial class SourceFile
    {
        public bool IsContentMaterialized => m_Content != null;

        private string CoerceContent(string value)
        {
            return value ?? (m_Content = m_ContentSource?.GetString());
        }

        private TextSourceBase CoerceContentSource(TextSourceBase value)
        {
            return value ?? (m_ContentSource = m_Content);
        }
    }

    partial class DefinitionSearchModel
    {
        private bool CoerceExcludeFromDefaultSearch(bool? value)
        {
            // Definitions must be stored even if not contributing to search to allow
            // other operations like tooltips/showing symbol name for find all references
            // so we just set ExcludeFromDefaultSearch to true
            return m_ExcludeFromDefaultSearch ??= (Definition.ExcludeFromSearch || Definition.ExcludeFromDefaultSearch);
        }
    }

    [ExcludedSerializationProperty(nameof(ReferenceKind))]
    partial class DefinitionSymbol : IShouldSerializeProperty<IDefinitionSymbol>
    {
        public void IncrementReferenceCount()
        {
            Interlocked.Increment(ref m_ReferenceCount);
        }

        private int CoerceReferenceCount(int value)
        {
            return value;
        }

        protected override ReferenceKind CoerceReferenceKind(ReferenceKind value)
        {
            return ReferenceKind.Definition;
        }

        protected override void Initialize()
        {
            base.Initialize();
            ReferenceKind = ObjectModel.ReferenceKind.Definition;
        }

        public static bool ShouldSerializeProperty(IDefinitionSymbol obj, string propertyName)
        {
            switch (propertyName)
            {
                case nameof(ReferenceKind):
                    return false;
                case nameof(ShortName):
                    return !string.IsNullOrEmpty(obj.ShortName);
            }

            return true;
        }

        private string CoerceShortName(string value)
        {
            return value ?? "";
        }

        private string CoerceAbbreviatedName(string value)
        {
            if (value == null && ShortName != null)
            {
                int abbreviationLength = GetAbbreviationLength();
                if (abbreviationLength >= 3)
                {
                    AbbreviatedName = value = this.AccumulateAbbreviationCharacters(
                        new StringBuilder(abbreviationLength),
                        t => t.accumulated.Append(t.ch)).ToString();
                }
            }

            return value;
        }

        private int GetAbbreviationLength()
        {
            return this.AccumulateAbbreviationCharacters(0, t => t.accumulated + 1);
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public partial class Span
    {
        /// <summary>
        /// The absolute character offset of the end (exclusive) of the span within the document
        /// </summary>
        public int End => Start + Length;

        public static readonly IComparer<ISpan> StartAndLengthComparer = new ComparerBuilder<ISpan>()
            .CompareByAfter(s => s.Start)
            .CompareByAfter(s => s.Length);
    }

    partial class ClassificationSpan
    {
        protected override void Initialize()
        {
            DefaultClassificationColor = -1;
            base.Initialize();
        }

        public override string ToString()
        {
            return $"[{Start}..{End}]({Classification}, Depth:{SymbolDepth}, Id:{LocalGroupId})";
        }
    }

    partial class LineSpan : IShouldSerializeProperty<ILineSpan>
    {
        public static bool ShouldSerializeProperty(ILineSpan obj, string propertyName)
        {
            switch (propertyName)
            {
                case nameof(LineNumber):
                    return obj.LineNumber > 1;
            }

            return true;
        }

        private int CoerceLineIndex(int? value)
        {
            if (value == null || (value <= 0 && m_LineNumber != null))
            {
                if (m_LineNumber != null)
                {
                    // Line number is 1-based whereas this value is 0-based
                    return m_LineNumber.Value - 1;
                }
                else
                {
                    return 0;
                }
            }

            return value.Value;
        }

        private int CoerceLineNumber(int? value)
        {
            if (value == null || (value == 1 && m_LineIndex != null))
            {
                if (m_LineIndex != null)
                {
                    // Line index is 0-based whereas this value is 1-based
                    return m_LineIndex.Value + 1;
                }
                else
                {
                    return 1;
                }
            }

            return value.Value;
        }

        protected override void OnDeserializedCore()
        {
            base.OnDeserializedCore();
        }

        protected override void OnSerializingCore()
        {
            base.OnSerializingCore();
        }
    }

    partial class TextLineSpan
    {
        public int LineSpanEnd => LineSpanStart + Length;

        public CharString Segment => this.GetSegment();

        public void Trim()
        {
            if (LineSpanText.IsNullOrWhitespace())
            {
                LineSpanStart = 0;
                LineSpanText = string.Empty;
                Length = 0;
            }
            else
            {
                var initialLength = LineSpanText.Length;
                LineSpanText = LineSpanText.TrimStart();
                var newLength = LineSpanText.Length;
                LineSpanStart -= (initialLength - newLength);
                LineSpanText = LineSpanText.TrimEnd();
                LineSpanStart = Math.Max(LineSpanStart, 0);
                Length = Math.Min(LineSpanText.Length, Length);
            }
        }

        public override string ToString()
        {
            return $"[{Start}, {End}] '{string.Concat(this.GetRichTextSpans())}'";
        }
    }

    public record struct RichText(ReadOnlyMemory<char> Text, bool Highlighted = false, Extent Range = default)
    {
        public override string ToString()
        {
            if (Highlighted)
            {
                return $"*{Text}*";
            }

            return Text.ToString();
        }
    }

    partial class ReferenceSpan
    {
        public override string ToString()
        {
            return $"[{Start}, {End}] {Reference?.ProjectId}::{Reference?.Id} '{string.Concat(this.GetRichTextSpans())}'";
        }
    }

    partial class SymbolSpan
    {
        public ReferenceSpan CreateReference(ReferenceSymbol referenceSymbol, SymbolId relatedDefinition = default(SymbolId))
        {
            return new ReferenceSpan(this)
            {
                RelatedDefinition = relatedDefinition,
                Reference = referenceSymbol
            };
        }

        public DefinitionSpan CreateDefinition(DefinitionSymbol definition)
        {
            return new DefinitionSpan(this)
            {
                Definition = definition
            };
        }
    }

    public partial class PropertyMap : Dictionary<StringEnum<PropertyKey>, string>, IPropertyMap
    {
    }

    //partial class TextSourceSearchModel
    //{
    //    protected override void OnDeserializedCore()
    //    {
    //        base.OnDeserializedCore();
    //    }

    //    protected override void OnSerializingCore()
    //    {
    //        File.Content = 
    //        base.OnSerializingCore();
    //    }
    //}

    partial class ReferenceSearchModel : IStandardEnumerable<ReferenceSearchResult>
    {
        public IDefinitionSymbol Definition => Symbol.AsDefinition();

        public IReadOnlyList<RelatedDefinition> RelatedDefinitions { get; set; }

        public IEnumerator<ReferenceSearchResult> GetEnumerator()
        {
            foreach (var span in Spans)
            {
                yield return new ReferenceSearchResult()
                {
                    File = FileInfo,
                    ReferenceSpan = span
                };
            }
        }

        private IReadOnlyList<IReferenceSpan> CoerceSpans(IReadOnlyList<IReferenceSpan> value)
        {
            if ((value == null || value.Count == 0) && References?.Spans.Count > 0)
            {
                value = References?.Spans.Where(s => !s.Info.ExcludeFromSearch).Select(s => s.ToReferenceSpan(References.Symbol)).ToList();
            }
            this.Spans = value ?? Array.Empty<IReferenceSpan>();
            return value;
        }
    }

    partial class SymbolReferenceList
    {
        public static IComparer<ISymbolReferenceList> Comparer { get; } = new ComparerBuilder<ISymbolReferenceList>()
            .CompareByAfter(l => l.Symbol, CodeSymbol.SymbolComparer);

        private IReadOnlyList<SharedReferenceInfoSpan> CoerceSpans(IReadOnlyList<SharedReferenceInfoSpan> value)
        {
            value = value ?? CompressedSpans?.ToList();
            this.Spans = value;
            return value;
        }

        protected override void OnSerializingCore()
        {
            if (Spans != null)
            {
                CharString lineSpanText = default;
                foreach (var span in Spans)
                {
                    Placeholder.Todo("Is this logic still needed");
                    span.LineSpanText = RemoveDuplicate(span.LineSpanText, ref lineSpanText);
                }
            }

            base.OnSerializingCore();
        }

        protected override void OnDeserializedCore()
        {
            if (Spans != null)
            {
                CharString lineSpanText = null;
                foreach (var span in Spans)
                {
                    span.LineSpanText = AssignDuplicate(span.LineSpanText, ref lineSpanText);
                }
            }

            base.OnDeserializedCore();
        }
    }
}