using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Codex.ObjectModel;
using Codex.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Codex.Analysis
{
    public class BoundSourceFileBuilder
    {
        private static readonly SourceHashAlgorithm[] s_checksumAlgorithms = new SourceHashAlgorithm[]
        {
            SourceHashAlgorithm.Sha1,
            SourceHashAlgorithm.Sha256
        };

        public static ComparerBuilder<ClassificationSpan> ClassificationSorter = new ComparerBuilder<ClassificationSpan>()
            .CompareByAfter(c => c, Span.StartAndLengthComparer)
            .CompareByAfter(c => c.Classification.Value.IsOutliningClassification() ? 0 : 1)
            .CompareByAfter(c => c.Classification.IntegralValue ?? int.MaxValue);

        public readonly BoundSourceFile BoundSourceFile = new BoundSourceFile();
        private readonly List<ReferenceSpan> references;
        private readonly List<ClassificationSpan> classifications;
        private readonly List<DefinitionSpan> definitions;

        public List<ClassificationSpan> Classifications => classifications;

        private bool isBuilt = false;
        private StringBuilder stringBuilder;
        public StringBuilder StringBuilder
        {
            get
            {
                if (stringBuilder == null)
                {
                    stringBuilder = new StringBuilder(SourceFile.Content);
                }

                return stringBuilder;
            }
        }

        public DefinitionSymbol FileDefinitionSymbol { get; }

        public readonly SourceFile SourceFile = new SourceFile();
        public readonly string ProjectId;

        private SourceText sourceText;
        public SourceText SourceText
        {
            get
            {
                if (stringBuilder != null)
                {
                    throw new InvalidOperationException();
                }

                if (sourceText == null)
                {
                    sourceText = SourceText.From(SourceFile.Content);
                }

                return sourceText;
            }

            set
            {
                if (stringBuilder != null)
                {
                    throw new InvalidOperationException();
                }

                if (SourceFile.Content != null)
                {
                    throw new InvalidOperationException("SourceText can only be set if Content is not set in SourceFile");
                }

                sourceText = value;
                SourceFile.Content = sourceText.ToString();
            }
        }

        public HashSet<ReferenceSpan> ReferencesSet = new HashSet<ReferenceSpan>(new EqualityComparerBuilder<ReferenceSpan>()
            .CompareByAfter(s => s.LineSpanText)
            .CompareByAfter(s => s.Start));

        public BoundSourceFileBuilder(SourceFileInfo sourceFileInfo, string projectId)
            : this(new SourceFile() { Info = sourceFileInfo, Content = string.Empty }, projectId)
        {
        }

        public BoundSourceFileBuilder(SourceFile sourceFile, string projectId)
        {
            BoundSourceFile.References = references = BoundSourceFile.References as List<ReferenceSpan> ?? new();
            BoundSourceFile.Classifications = classifications = BoundSourceFile.Classifications as List<ClassificationSpan> ?? new();
            BoundSourceFile.Definitions = definitions = BoundSourceFile.Definitions as List<DefinitionSpan> ?? new();

            BoundSourceFile.SourceFile = sourceFile;
            BoundSourceFile.ProjectId = projectId;
            this.SourceFile = sourceFile;
            this.ProjectId = projectId;

            var definitionSymbol = CreateFileDefinitionSymbol(sourceFile.Info.ProjectRelativePath, projectId);
            FileDefinitionSymbol = definitionSymbol;

            AnnotateDefinition(0, 0, definitionSymbol, isImplicit: true);
        }

        public static DefinitionSymbol CreateFileDefinitionSymbol(string logicalPath, string projectId)
        {
            return new DefinitionSymbol(CreateFileReferenceSymbol(logicalPath, projectId, isDefinition: true))
            {
                ShortName = PathUtilities.GetFileName(logicalPath),
                ContainerQualifiedName = PathUtilities.GetDirectoryName(logicalPath),
            };
        }

        public static ReferenceSymbol CreateFileReferenceSymbol(string logicalPath, string projectId, bool isDefinition = false)
        {
            return new ReferenceSymbol()
            {
                Id = SymbolId.CreateFromId(logicalPath.ToLowerInvariant()),
                ProjectId = projectId,
                ReferenceKind = isDefinition ? ReferenceKind.Definition : ReferenceKind.Reference,
                Kind = SymbolKinds.File,
            };
        }

        public void AnnotateClassification(int start, int length, string classification)
        {
            classifications.Add(new ClassificationSpan()
            {
                Start = start,
                Length = length,
                Classification = classification
            });
        }

        public void AnnotateReferences(int start, int length, params ReferenceSymbol[] refs)
        {
            foreach (var reference in refs)
            {
                // NOTE: not all data is provided here! There is a post processing step to populate it.
                references.Add(new ReferenceSpan()
                {
                    Start = start,
                    Length = length,
                    Reference = reference
                });
            }
        }

        public void AddReference(ReferenceSpan span)
        {
            references.Add(span);
        }

        public void AddReferences(IEnumerable<ReferenceSpan> spans)
        {
            references.AddRange(spans);
        }

        public void AddClassifications(IEnumerable<ClassificationSpan> spans)
        {
            classifications.AddRange(spans);
        }

        public void AddDefinition(DefinitionSpan span)
        {
            definitions.Add(span);
        }

        public void AddDefinitions(IEnumerable<DefinitionSpan> spans)
        {
            definitions.AddRange(spans);
        }

        public void AnnotateDefinition(int start, int length, DefinitionSymbol definition, bool isImplicit = false, bool addReference = true)
        {
            definitions.Add(new DefinitionSpan()
            {
                Definition = definition,
                Start = start,
                Length = length
            });

            references.Add(new ReferenceSpan()
            {
                Start = start,
                Length = length,
                Reference = definition,
                IsImplicitlyDeclared = isImplicit
            });
        }

        public void AppendReferences(string text, params ReferenceSymbol[] referenceSymbols)
        {
            SymbolSpan symbolSpan = new SymbolSpan()
            {
                Start = StringBuilder.Length,
                Length = text.Length,
            };

            StringBuilder.Append(text);
            foreach (var reference in referenceSymbols)
            {
                references.Add(symbolSpan.CreateReference(reference));
            }
        }

        public void AppendDefinition(string text, DefinitionSymbol definition)
        {
            SymbolSpan symbolSpan = new SymbolSpan()
            {
                Start = StringBuilder.Length,
                Length = text.Length,
            };

            StringBuilder.Append(text);
            definitions.Add(symbolSpan.CreateDefinition(definition));
            references.Add(symbolSpan.CreateReference(definition));
        }

        public BoundSourceFile Build()
        {
            if (isBuilt)
            {
                return BoundSourceFile;
            }

            isBuilt = true;

            if (stringBuilder != null)
            {
                SourceFile.Content = stringBuilder.ToString();
                stringBuilder = null;
            }

            SourceText text = SourceText;
            var info = BoundSourceFile.SourceFile.Info;
            info.Properties = info.Properties ?? new PropertyMap();
            info.Lines = text.Lines.Count;

            foreach (var checksumAlgorithm in s_checksumAlgorithms)
            {
                if (GetChecksumKey(checksumAlgorithm) is { } checksumKey)
                {
                    var checksumText = text.ChecksumAlgorithm == checksumAlgorithm
                        ? text
                        : new ChecksumSourceText(text, checksumAlgorithm);
                    var checksum = checksumText.GetChecksum().ToHex();
                    info.Properties[checksumKey] = checksum;

                    //AnnotateDefinition(0, 0,
                    //    new DefinitionSymbol()
                    //    {
                    //        Id = SymbolId.CreateFromId($"{checksumKey}|{checksum}"),
                    //        DisplayName = checksum,
                    //        // Use abbreviated name and no short name instead of short name to ensure
                    //        // the name isn't broken up into suffixes
                    //        ShortName = null,
                    //        AbbreviatedName = checksum,
                    //        ProjectId = BoundSourceFile.ProjectId,
                    //        ReferenceKind = ReferenceKind.Definition,
                    //        Kind = checksumKey.ToString(),
                    //        IsImplicitlyDeclared = true
                    //    });
                }
            }

            classifications.Sort(ClassificationSorter);
            references.Sort(CodeSymbol.SymbolSpanSorter);
            definitions.Sort(CodeSymbol.SymbolSpanSorter);

            ReferenceSpan lastReference = null;

            Placeholder.Todo("Remove line span text and use spans of source content for Trim()");
            foreach (var reference in references)
            {
                try
                {
                    if (lastReference?.Start == reference.Start)
                    {
                        reference.LineIndex = lastReference.LineIndex;
                        reference.LineSpanStart = lastReference.LineSpanStart;
                        reference.LineSpanText = lastReference.LineSpanText;
                        continue;
                    }

                    var line = text.Lines.GetLineFromPosition(reference.Start);
                    if (lastReference?.LineIndex == line.LineNumber)
                    {
                        reference.LineIndex = line.LineNumber;
                        reference.LineSpanStart = lastReference.LineSpanStart + (reference.Start - lastReference.Start);
                        reference.LineSpanText = lastReference.LineSpanText;
                        continue;
                    }

                    reference.LineIndex = line.LineNumber;
                    reference.LineSpanStart = reference.Start - line.Start;
                    reference.LineSpanText = line.ToString();
                    reference.Trim();
                }
                finally
                {
                    lastReference = reference;
                }
            }

            foreach (var definitionSpan in BoundSourceFile.Definitions)
            {
                definitionSpan.Definition.ProjectId = definitionSpan.Definition.ProjectId ?? ProjectId;
            }

            return BoundSourceFile;
        }

        private class ChecksumSourceText : SourceText
        {
            private SourceText inner;
            public ChecksumSourceText(SourceText inner, SourceHashAlgorithm checksumAlgorithm)
                : base(checksumAlgorithm: checksumAlgorithm)
            {
                this.inner = inner;
            }

            public override char this[int position] => inner[position];

            public override Encoding Encoding => inner.Encoding;

            public override int Length => inner.Length;

            public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
            {
                inner.CopyTo(sourceIndex, destination, destinationIndex, count);
            }
        }

        private static PropertyKey? GetChecksumKey(SourceHashAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case SourceHashAlgorithm.Sha1:
                    return PropertyKey.Checksum_Sha1;
                case SourceHashAlgorithm.Sha256:
                    return PropertyKey.Checksum_Sha256;
                default:
                    return null;
            }
        }
    }
}