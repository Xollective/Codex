using System.Collections.Concurrent;
using Codex.Lucene.Framework.AutoPrefix;
using CommunityToolkit.HighPerformance;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Codex.Lucene.Framework;

public record ProjectReferenceScoreState(ProjectReferenceMinCountSketch Sketch) : IDocValuesScoreState
{
    public ConcurrentDictionary<BytesRefString, uint> ProjectScoreCache { get; } = new();

    public float GetScore(BytesRefString project)
    {
        if (!ProjectScoreCache.TryGetValue(project, out var score))
        {
            score = Sketch.Get(project.GetString());
            var key = new BytesRef();
            key.CopyBytes(project);

            ProjectScoreCache.TryAdd(key, score);
        }

        return score * 1000;
    }
}