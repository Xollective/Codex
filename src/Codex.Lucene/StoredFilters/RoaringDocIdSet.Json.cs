using System.Text.Json.Serialization;
using Codex.Utilities;
using Codex.Utilities.Serialization;

namespace Codex.Lucene.Formats
{
    public partial class RoaringDocIdSet : IJsonConvertible<RoaringDocIdSet, PersistedIdSet>
    {
        public static RoaringDocIdSet ConvertFromJson(PersistedIdSet jsonFormat)
        {
            var result = RoaringDocIdSet.FromPersisted(jsonFormat);
            if (Features.RebuildDocIdSetsOnLoad)
            {
                result = RoaringDocIdSet.From(result.Enumerate());
            }

            return result;
        }

        public PersistedIdSet ConvertToJson()
        {
            return ToPersisted();
        }
    }
}
