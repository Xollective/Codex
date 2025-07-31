using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Codex.Lucene;
using Codex.ObjectModel;
using Codex.Storage;
using Codex.Utilities;
using Codex.Utilities.Serialization;
using Lucene.Net;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Tenray.ZoneTree;
using Tenray.ZoneTree.Core;
using Tenray.ZoneTree.Options;
using Tenray.ZoneTree.Serializers;
using Xunit.Abstractions;
using static Lucene.Net.Util.Packed.PackedInt32s;

namespace Codex.Integration.Tests;

public record ZoneTreeTests(ITestOutputHelper Output) : CodexTestBase(Output)
{
    [Fact]
    public async Task TestZoneTree()
    {
        var output = GetTestOutputDirectory();
        PathUtilities.ForceDeleteDirectory(output);

        var comparer = new StructBytesSerializer<MurmurHash>();

        int constrain(int value) => Math.Min(1, Math.Max(-1, value));

        for (int i = 0; i < 100; i++)
        {
            var h1 = MurmurHash.Random();
            var h2 = MurmurHash.Random();

            var result = constrain(comparer.Compare(h1, h2));

            var result2 = constrain(h1.High.ChainCompareTo(h2.High) ?? h1.Low.CompareTo(h2.Low));

            if (result != result2)
            {

            }
        }


        var tree = new ZoneTreeFactory<MurmurHash, DocumentRef>()
            .SetDataDirectory(output)
            .ConfigureWriteAheadLogOptions(o => o.WriteAheadLogMode = WriteAheadLogMode.None)
            .SetKeySerializer(new StructSerializer<MurmurHash>())
            .SetValueSerializer(new StructSerializer<DocumentRef>())
            .SetComparer(new StructBytesSerializer<MurmurHash>())
            .Create();

        var key1 = MurmurHash.Random();

        Assert.False(tree.ContainsKey(key1));

        tree.Upsert(key1, new(20));

        tree.Upsert(key1, new(21));

        bool found = tree.TryGet(key1, out var value);


    }

    [Fact]
    public async Task TestZoneTreeDiskRoundTrip()
    {
        var output = GetTestOutputDirectory();
        PathUtilities.ForceDeleteDirectory(output);

        int docRef = 0;
        var header = new StableIdStorageHeader();
        Dictionary<MurmurHash, DocumentRef> keys = new Dictionary<MurmurHash, DocumentRef>();
        for (int i = 0; i < 10; i++)
        {
            await using var storage = new ZoneTreeStableIdStorage(output);
            storage.Initialize(header);

            var key = MurmurHash.Random();
            var value = new DocumentRef(docRef++);

            keys[key] = value;
            storage.UnsafePut(SearchTypes.Reference, key, value);
            storage.UnsafePut(SearchTypes.BoundSource, key, value);

            foreach (var kvp in keys)
            {
                if (storage.TryGet(SearchTypes.Reference, kvp.Key, out var readValue))
                {
                    Assert.Equal(kvp.Value, readValue);
                }
                else
                {
                    Assert.Fail($"Cound not find key: {kvp.Key}");
                }
            }
        }
    }

    [Fact]
    public async Task Test()
    {
        var path = GetTestOutputDirectory();
        PathUtilities.ForceDeleteDirectory(path);

        for (int i = 0; i < 20; i++)
        {
            var key = MurmurHash.Random();
            var entityUid = MurmurHash.Random();
            DocumentRef docRef1 = default;

            var header = new StableIdStorageHeader();
            await using (var storage = new ZoneTreeStableIdStorage(path))
            {
                storage.Initialize(header);

                Assert.True(storage.TryReserve(SearchTypes.BoundSource, entityUid, out docRef1));
                Assert.False(storage.TryReserve(SearchTypes.BoundSource, entityUid, out var docRef2));
                Assert.True(storage.TryReserve(SearchTypes.BoundSource, MurmurHash.Random(), out var docRef3));

                Assert.Equal(docRef1, docRef2);
                Assert.Equal(docRef1.DocId + 1, docRef3.DocId);
            }

            await using (var storage = new ZoneTreeStableIdStorage(path))
            {
                storage.Initialize(header);

                using var iterator = storage.Database.CreateIterator(IteratorType.NoRefresh);
                var values = iterator.AsEnumerable().ToList();

                Assert.False(storage.TryReserve(SearchTypes.BoundSource, entityUid, out var docRef2));
                Assert.True(storage.TryReserve(SearchTypes.BoundSource, MurmurHash.Random(), out var docRef3));
                Assert.Equal(docRef1, docRef2);
                Assert.Equal(docRef1.DocId + 2, docRef3.DocId);
            }
        }
    }

}