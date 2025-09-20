using System.Runtime.CompilerServices;
using Codex.Utilities.Serialization;

namespace Codex.ObjectModel.Attributes
{
    /// <summary>
    /// Specifies the type of search behavior for a field.
    /// </summary>
    [GeneratorExclude]
    public enum SearchBehavior
    {
        /// <summary>
        /// No search behavior.
        /// </summary>
        None,

        /// <summary>
        /// Standard term search (exact match).
        /// </summary>
        Term,

        /// <summary>
        /// Search using a normalized keyword (case-insensitive, trimmed, etc.).
        /// </summary>
        NormalizedKeyword,

        /// <summary>
        /// Search using a sortword (normalized for sorting purposes).
        /// </summary>
        Sortword,

        /// <summary>
        /// Full-text search (tokenized, supports partial matches).
        /// </summary>
        FullText,

        /// <summary>
        /// Search using a prefix term (matches terms starting with the prefix).
        /// </summary>
        PrefixTerm,

        /// <summary>
        /// Search using a prefix of a short name (e.g., symbol short name).
        /// </summary>
        PrefixShortName,

        /// <summary>
        /// Search using a prefix of a full name or path (e.g., symbol full name).
        /// </summary>
        PrefixFullName,

        /// <summary>
        /// Search using a sort value (optimized for sorting).
        /// </summary>
        SortValue,
    }

    [GeneratorExclude]
    public enum SearchBehaviorFlags
    {
        None = 0,
        CanSearchPartialTerm = 1 << 0,
        DisallowSummarizeFullTerm = 1 << 1,
        PreferBinary = 1 << 2,
        IsSymbolId = 1 << 3,
        IsHashExcluded = 1 << 4,
        IsStableId = 1 << 5,
        LowCardinalityTermOptimization = 1 << 6,
        IsPath = 1 << 7,
        IsStableIdRef = 1 << 8
    }

    public record SearchBehaviorInfo(
        SearchBehavior? Behavior = null,
        SearchBehaviorFlags Flags = SearchBehaviorFlags.None)
    {
        public bool CanSearchPartialTerm => Flags.HasFlag(SearchBehaviorFlags.CanSearchPartialTerm);
        public bool DisallowSummarizeFullTerm => Flags.HasFlag(SearchBehaviorFlags.DisallowSummarizeFullTerm);
        public bool PreferBinary => Flags.HasFlag(SearchBehaviorFlags.PreferBinary);
        public bool IsSymbolId => Flags.HasFlag(SearchBehaviorFlags.IsSymbolId);
        public bool IsHashExcluded => Flags.HasFlag(SearchBehaviorFlags.IsHashExcluded);
        public bool IsStableId => Flags.HasFlag(SearchBehaviorFlags.IsStableId);
        public bool LowCardinalityTermOptimization => Flags.HasFlag(SearchBehaviorFlags.LowCardinalityTermOptimization);
        public bool IsPath => Flags.HasFlag(SearchBehaviorFlags.IsPath);
        public bool IsStableIdRef => Flags.HasFlag(SearchBehaviorFlags.IsStableIdRef);

        public bool TryGetBinaryValue(string value, out ValueArray<byte, T256> binaryValue)
        {
            binaryValue = ValueArrayLength.MaxCapacity;
            if (!PreferBinary) return false;

            if (IsSymbolId)
            {
                if (SymbolId.UnsafeCreateWithValue(value).TryGetBinaryValue(out var idValue))
                {
                    SpanWriter writer = binaryValue.GetValuesSpan();
                    writer.Write(idValue);
                    binaryValue.Length = writer.Position;
                    return true;
                }

                return false;
            }

            return false;
        }

        public SearchBehaviorInfo WithAdditionalFlags(SearchBehaviorFlags flags)
        {
            return this with { Flags = Flags | flags };
        }
    }
}