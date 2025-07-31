using Codex.Lucene;
using Codex.Lucene.Framework;
using Codex.Lucene.Search;
using Codex.ObjectModel;
using Codex.ObjectModel.Implementation;
using Codex.Utilities;
using CommunityToolkit.HighPerformance;
using Lucene.Net.Index;

namespace Codex.Integration.Tests.Lucene;

using D = SearchMappings.Definition;

public partial record LuceneTests
{
    [Fact]
    public async Task TestAutoPrefixWithDictionary()
    {
        var dir = GetTestOutputDirectory(clean: true);

        var dict = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Large);
        var words = dict.GetAllWords().Take(0).ToList();
        words.Add("putfilerequest");
        words.Add("putfilerequest");
        words.Add("PutFileRequiringNewReplicaCloseToHardLimitDoesNotHang".ToLowerInvariant());

        var luceneStore = new LuceneCodexStore(new LuceneWriteConfiguration(dir));

        var dw = luceneStore.Writers[SearchTypes.Definition];
        //await TaskUtilities.ForEachAsync(true, words.WithIndices(), (i, token) =>
        //{
        //    var index = i.Index;
        //    dw.Add(new DefinitionSymbol() { ShortName = i.Item }, commit: false);
        //    if ((index % 10000) == 0) dw.Commit();
        //    return ValueTask.CompletedTask;
        //});

        dw.AddSimpleDef("putfilerequest", commit: true);
        dw.AddSimpleDef("PutFileRequiringNewReplicaCloseToHardLimitDoesNotHang", commit: true);
        dw.ForceMerge(1, true);


        dw.AddSimpleDef("putfilerequest", commit: true);
        dw.ForceMerge(1, true);

        var reader = dw.GetReader(true);

        var r = SlowCompositeReaderWrapper.Wrap(reader);

        var terms = r.GetTerms(D.ShortName.Name);
        var te = terms.GetEnumerator();

        var tl = te.Enumerate().SelectValues().ToArray();

        te.GoToExact("^putfilerequ").Should().BeTrue();
        var docs = te.Docs().Enumerate().ToArray();
    }
}