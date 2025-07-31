using Codex.Sdk.Utilities;
using Codex.Utilities;
using System.Diagnostics;
using System.Linq;
using static Codex.Utilities.SerializationUtilities;

namespace Codex.ObjectModel
{
    namespace Implementation
    {
        partial class BoundSourceSearchModel
        {
            public BoundSourceFile BoundFile { get; set; }
        }

        partial class BoundSourceInfo
        {
            private int CoerceReferenceCount(int? value)
            {
                return value ?? References.Count;
            }

            private int CoerceDefinitionCount(int? value)
            {
                return value ?? Definitions.Count;
            }
        }

        partial class BoundSourceFile
        {
            public static readonly ITypeBox<BoundSourceFile> Type = TypeBox.Get<BoundSourceFile>();

            public string IndexName { get; set; }

            public IEnumerable<SourceSpan> SourceSpans { get; set; }

            public BoundSourceFlags Flags
            {
                get => SourceFile.Flags;
                set => SourceFile.Flags = value;
            }

            public string ProjectId
            {
                get => SourceFile?.Info.ProjectId;
                set => SourceFile.Info.ProjectId = value;
            }

            public string RepoRelativePath
            {
                get => SourceFile?.Info.RepoRelativePath;
                set => SourceFile.Info.RepoRelativePath = value;
            }

            public string ProjectRelativePath
            {
                get => SourceFile?.Info.ProjectRelativePath;
                set => SourceFile.Info.ProjectRelativePath = value;
            }

            public string RepositoryName
            {
                get => SourceFile?.Info.RepositoryName;
                set => SourceFile.Info.RepositoryName = value;
            }

            public string Language
            {
                get => SourceFile?.Info.Language;
                set => SourceFile.Info.Language = value;
            }

            public bool ExcludeFromSearch
            {
                get => SourceFile.ExcludeFromSearch;
                set => SourceFile.ExcludeFromSearch = value;
            }

            public void ApplySourceFileInfo()
            {
                
            }
        }

        partial class BoundSourceSearchModel
        {
            protected override void OnDeserializedCore()
            {
                BindingInfo ??= new BoundSourceInfo();

                if (CompressedClassifications != null)
                {
                    BindingInfo.Classifications = Lazy.CreateList(() => CompressedClassifications.ToList());
                }

                if (References != null)
                {
                    BindingInfo.References = Lazy.CreateList(() => CreateReferenceList(References));
                }

                base.OnDeserializedCore();
            }

            private IReadOnlyList<ReferenceSpan> CreateReferenceList(List<SymbolReferenceList> referenceLists)
            {
                var references = referenceLists
                    .SelectMany(r => r.Spans.Select(s => s.ToReferenceSpan(r.Symbol)))
                    .OrderBy(r => r.Start)
                    .ThenBy(r => r.Length)
                    .ToList();

                return references;
            }

            protected override void OnSerializingCore()
            {
                base.OnSerializingCore();
            }
        }
    }
}