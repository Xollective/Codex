using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Codex.ObjectModel;
using Codex.ObjectModel.Attributes;
using Codex.Utilities;
using static Codex.Utilities.SerializationUtilities;

namespace Codex.ObjectModel.Implementation
{
    [DataContract]
    public class SharedReferenceInfoSpanModel : 
        SpanListModel<SharedReferenceInfoSpan, SpanListSegmentModel, SharedReferenceInfo, SharedReferenceInfo>, 
        ISharedReferenceInfoSpanModel
    {
        public static readonly EqualityComparerBuilder<SharedReferenceInfo> ReferenceInfoEquator = 
            new EqualityComparerBuilder<SharedReferenceInfo>()
            .CompareByAfter(s => s.ReferenceKind)
            .CompareByAfter(s => s.RelatedDefinition.Value)
            .CompareByAfter(s => s.ExcludeFromSearch)
            ;

        public static readonly IComparer<SharedReferenceInfo> ReferenceInfoComparer = 
            new ComparerBuilder<SharedReferenceInfo>()
            .CompareByAfter(s => s.ReferenceKind)
            .CompareByAfter(s => s.RelatedDefinition.Value)
            .CompareByAfter(s => s.ExcludeFromSearch)
            ;

        private static readonly SymbolSpan EmptySymbolSpan = new SymbolSpan();

        [DataMember(Order = 20)]
        public SymbolLineSpanListModel LineSpanModel { get; set; }

        IReadOnlyList<ISharedReferenceInfo> ISharedReferenceInfoSpanModel.SharedValues => SharedValues;

        [DataMember(Order = 21)]
        public IntegerListModel LineIndices;

        public SharedReferenceInfoSpanModel()
        {
        }

        public SharedReferenceInfoSpanModel(IReadOnlyList<SharedReferenceInfoSpan> spans, bool includeLineInfo = false, bool externalLineTextPersistence = false)
            : base(spans, ReferenceInfoEquator, ReferenceInfoComparer)
        {
            if (includeLineInfo)
            {
                LineSpanModel = new SymbolLineSpanListModel(spans, useOrdinalSort: externalLineTextPersistence)
                {
                    // Start/length already captured. No need for it in the line data
                    IncludeSpanRanges = false
                };

                if (externalLineTextPersistence)
                {
                    LineIndices = IntegerListModel.Create(LineSpanModel.SharedValues, span => span.LineIndex);
                }
            }
        }

        public static SharedReferenceInfoSpanModel CreateFrom(IEnumerable<ReferenceSpan> spans, bool includeLineInfo = true, bool externalLineTextPersistence = false)
        {
            return new SharedReferenceInfoSpanModel(spans.Select(SharedReferenceInfoSpan.From).ToList(), includeLineInfo, externalLineTextPersistence);
        }

        [OnSerializing]
        public void PostProcessReferences(StreamingContext context)
        {
            if (LineSpanModel != null && LineIndices != null)
            {
                LineSpanModel.SharedValues.Clear();

                if (Optimize)
                {
                    LineIndices.Optimize(new OptimizationContext());
                }
            }
        }

        [Placeholder.Todo("Is this being called?")]
        [OnDeserialized]
        public void MakeReferences(StreamingContext context)
        {
            if (LineSpanModel != null && LineIndices != null)
            {
                if (LineIndices.CompressedData != null)
                {
                    LineIndices.ExpandData(new OptimizationContext());
                }

                for (int i = 0; i < LineIndices.Count; i++)
                {
                    LineSpanModel.SharedValues.Add(new SymbolSpan()
                    {
                        LineIndex = LineIndices[i]
                    });
                }
            }
        }

        public override SpanListSegmentModel CreateSegment(ListSegment<SharedReferenceInfoSpan> segmentSpans)
        {
            return new SpanListSegmentModel();
        }

        public override SharedReferenceInfoSpan CreateSpan(int start, int length, SharedReferenceInfo shared, SpanListSegmentModel segment, int segmentOffset)
        {
            var index = segment.SegmentStartIndex + segmentOffset;
            var lineSpan = LineSpanModel?.GetShared(index) ?? EmptySymbolSpan;

            return new SharedReferenceInfoSpan(lineSpan)
            {
                Start = start,
                Length = length,
                Info = shared,
                LineSpanStart = start - lineSpan.Start,
            };
        }

        public override SharedReferenceInfo GetShared(SharedReferenceInfoSpan span)
        {
            return GetSharedKey(span);
        }

        public override SharedReferenceInfo GetSharedKey(SharedReferenceInfoSpan span)
        {
            return span.Info;
        }
    }
}
