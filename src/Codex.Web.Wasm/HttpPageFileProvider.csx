using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Codex.Lucene.Search;
using Codex.ObjectModel.Implementation;
using Codex.Utilities;
using Lucene.Net.Codecs;

namespace Codex.Web.Wasm
{
    internal record HttpPageFileProvider(string IndexRootUrl, HttpClient Client) : IPageFileProvider
    {
        public PagingDirectoryInfo Info { get; private set; }

        public async Task InitializeAsync()
        {
            var pagingInfoUrl = PathUtilities.UriCombine(IndexRootUrl, LuceneConstants.FilesJsonRelativePath);
            WriteLine($"Downloading info: {pagingInfoUrl}");
            var infoJson = await Client.GetStringAsync(pagingInfoUrl);

            WriteLine($"Reading info: {pagingInfoUrl} : Contents: {infoJson}");
            Info = JsonSerializationUtilities.DeserializeEntity<PagingDirectoryInfo>(infoJson);

            WriteLine($"Read Entries:{Info.Entries.Count}");
        }

        public IPageFile CreatePageFile(string path, PagingFileEntry entry)
        {
            WriteLine($"Creating page file: Length={entry.Length} Path={path}");
            return new HttpPageFile(this, path, entry);
        }

        public static void WriteLine(string message)
        {
            var ct = Thread.CurrentThread;
            string now = DateTime.UtcNow.ToString();
            Console.WriteLine($"[{now}] T{ct.ManagedThreadId} -- {message}");
        }

        public class HttpPageFile : IPageFile
        {
            public HttpPageFile(HttpPageFileProvider provider, string path, PagingFileEntry entry)
            {
                Provider = provider;
                Path = PathUtilities.UriCombine(provider.IndexRootUrl, path.Replace('\\', '/'));
                Entry = entry;
            }

            public long Length => Entry.Length;

            public HttpPageFileProvider Provider { get; }
            public string Path { get; }
            public PagingFileEntry Entry { get; }

            public void Dispose()
            {
            }

            public void ReadRange(long position, byte[] buffer, int offset, int count)
            {
                WriteLine($"Reading range pos={position} count={count} Path={Path}");

                if (BrowserAppContext.IsMainThread)
                {
                    Console.Error.WriteLine("ReadRange should not be called on Main thread");
                }

                try
                {
                    var bytes = Provider.Client.GetByteRangeAsync(Path, position, count).GetAwaiter().GetResult();

                    WriteLine($"Read range bytes={bytes.Length} pos={position} count={count} Path={Path}");

                    bytes.AsSpan(0, count).CopyTo(buffer.AsSpan(offset, count));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"ReadRange.Exception ({Path}:{position},{count}): {ex}");
                }
            }
        }
    }
}
