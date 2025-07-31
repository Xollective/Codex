using Codex.Logging;
using Codex.Lucene.Search;
using Codex.ObjectModel;
using Codex.Storage;
using Codex.Storage.BlockLevel;
using Codex.Utilities.Zip;
using Codex.Web.Common;
using Lucene.Net.Search;

namespace Codex
{
    /// <summary>
    /// Defines on/off state of experimental features
    /// </summary>
    public static class LuceneFeatures
    {
        public static readonly FeatureSwitch<Action<ILuceneIndex, Query, Filter>> OnQuery = new();

        public static readonly FeatureSwitch<int> IndexMergeLoadFactor = 2;
    }
}