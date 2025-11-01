using System.Buffers.Binary;
using System.Diagnostics.ContractsLight;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Codex.Search;
using Codex.Utilities;
using Codex.Utilities.Serialization;
using J2N.Numerics;
using Lucene.Net;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Util;
using M = Codex.ObjectModel.Implementation.SearchMappings;

namespace Codex.Lucene.Search
{
    public record DocumentVisitor(Document Document) : ValueVisitorBase
    {
        public override bool HandlesNoneBehavior => false;

        public override void Visit(IMappingField mapping, TextSourceBase value)
        {
            if (mapping.Behavior == SearchBehavior.FullText)
            {
                Document.Add(new TextField(mapping.Name, value.GetReader()));
            }

            Visit(mapping, value.GetString());
        }

        public override void Visit(IMappingField mapping, string value, SearchBehaviorInfo info)
        {
            var behavior = info.Behavior ?? mapping.Behavior;
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (info.TryGetBinaryValue(value, out var binaryValue))
            {
                VisitBinaryItem(mapping, BinaryItem.Create(binaryValue.Values, binaryValue.Length));
                return;
            }

            for (int i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (!char.IsAscii(ch))
                {
                    value = getAsciiOnlyString(value);
                    break;
                }
            }

            string getAsciiOnlyString(string value)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < value.Length; i++)
                {
                    var ch = value[i];
                    sb.Append(char.IsAscii(ch) ? ch : ' ');
                }

                return sb.ToString();
            }

            switch (behavior)
            {
                case SearchBehavior.PrefixTerm:
                case SearchBehavior.PrefixShortName:
                    {
                        Placeholder.Todo("implement real fields for above search behaviors");
                        var processedValue = SearchUtilities.GetNameTransformedValue(value, lowercase: false);
                        var lowercaseValue = processedValue.ToLowerInvariant() + "$";

                        Placeholder.Todo("Do we need special handling for PrefixTerm");
                        if (behavior == SearchBehavior.PrefixShortName)
                        {
                            if (value.Contains('/'))
                            {
                                // Treat everything after last '/' as separate short name
                                // This primarily allows searching for repos using full name dotnet/runtime
                                // and short name runtime with both matching prefixes.
                                Visit(mapping, value.AsSpan().SubstringAfterLastIndexOfAny("/").ToString());
                            }

                            IndexingUtilities.AccumulateAbbreviationCharacters(processedValue, (Document, lowercaseValue, mapping), static t =>
                            {
                                var (doc, lowercaseValue, mapping) = t.accumulated;
                                if (t.index != 0) // don't index full string. That is indexed below with prepended '^' at beginning
                                {
                                    doc.Add(new StringField(mapping.Name, lowercaseValue.Substring(t.index), Field.Store.NO));
                                }

                                return t.accumulated;
                            });
                        }

                        Document.Add(new StringField(mapping.Name, "^" + lowercaseValue, Field.Store.NO));
                        break;
                    }
                case SearchBehavior.PrefixFullName:
                    {
                        Placeholder.Todo("Long term handling (hash full term)");
                        foreach (var processedValue in SearchUtilities.EnumerateContainerQualifiedNameFieldValues(value, info.IsPath))
                        {
                            Document.Add(new StringField(mapping.Name, processedValue, Field.Store.NO));
                        }
                        break;
                    }
                case SearchBehavior.Term:
                case SearchBehavior.NormalizedKeyword:
                case SearchBehavior.Sortword:
                case SearchBehavior.SortValue:
                    value = value.ToLowerInvariant();

                    if (behavior != SearchBehavior.SortValue)
                    {
                        if (TryGetShorterHash(value, out var hash))
                        {
                            Document.Add(hash.CreateBinaryValueField(mapping.Name));
                        }
                        else
                        {
                            Document.Add(new StringField(mapping.Name, value, Field.Store.NO));
                        }
                    }

                    if (behavior == SearchBehavior.Sortword || behavior == SearchBehavior.SortValue)
                    {
                        Document.Add(new SortedDocValuesField(mapping.Name, new BytesRef(value)));
                    }
                    break;
                case SearchBehavior.FullText:
                    
                    // TODO: If we don't store field. We probably need to do something against _source
                    // field for highlighting. Other option, is to just replay this field into the document
                    // when requested.
                    Document.Add(new TextField(mapping.Name, value, Field.Store.NO));
                    //doc.Add(new Field(mapping.Name, value, FullTextType));
                    break;
                default:
                    Contract.AssertFailure(
                        $"Field {mapping.Name} has unexpected search behavior '{behavior}'.");
                    break;
            }

        }

        public static bool TryGetShorterHash(ReadOnlySpan<char> value, out ShortHash hash)
        {
            hash = default;
            if (value.Length <= ShortHash.BYTE_LENGTH) return false;

            hash = IndexingUtilities.UnicodeHash(value);
            return true;
        }

        private FieldType Int64SortValueType = new FieldType(Int64Field.TYPE_NOT_STORED)
        {
            DocValueType = DocValuesType.NUMERIC,
            IsIndexed = false,
            IsTokenized = false,
            IndexOptions = IndexOptions.NONE
        };

        public override void Visit(IMappingField mapping, long value)
        {
            switch (mapping.Behavior)
            {
                case SearchBehavior.Term:
                case SearchBehavior.NormalizedKeyword:
                case SearchBehavior.Sortword:
                    var item = GetInt64Term(value);

                    Document.Add(item.CreateBinaryField(mapping.Name));

                    if (mapping.Behavior == SearchBehavior.Sortword)
                    {
                        Document.Add(new Int64Field(mapping.Name, value, Int64SortValueType));
                    }
                    break;
                case SearchBehavior.SortValue:
                    Document.Add(new Int64Field(mapping.Name, value, Int64SortValueType));
                    break;
                default:
                    Contract.AssertFailure(
                        $"Field {mapping.Name} has unexpected search behavior '{mapping.Behavior}'.");
                    break;
            }
        }

        public static BinaryItem.StructBinaryItem<Int128> GetInt64Term(long value)
        {
            Int128 largeValue = value;
            largeValue <<= 8;

            var item = BinaryItem.Create(largeValue);
            var span = item.GetSpan();
            var trimmedLength = span.TrimEnd((byte)0).Length;

            largeValue += trimmedLength;
            item = BinaryItem.Create(largeValue);
            item = item with { Length = trimmedLength };
            return item;
        }

        public override void VisitBinaryItem<T>(IMappingField mapping, T value)
        {
            switch (mapping.Behavior)
            {
                case SearchBehavior.Term:
                case SearchBehavior.NormalizedKeyword:
                    Document.Add(value.CreateBinaryField(mapping.Name));
                    return;
                case SearchBehavior.Sortword:
                    Document.Add(value.CreateBinaryField(mapping.Name));
                    Document.Add(new SortedDocValuesField(mapping.Name, value.ToBytes()));
                    return;
                default:
                    Contract.AssertFailure(
                        $"Field {mapping.Name} has unexpected search behavior '{mapping.Behavior}'.");
                    break;
            }

            
        }
    }
}
