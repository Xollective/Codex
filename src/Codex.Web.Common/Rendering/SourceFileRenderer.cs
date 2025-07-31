using System.Diagnostics.ContractsLight;
using System.Text;
using Codex.View;

namespace Codex.Web.Mvc.Rendering
{
    using static CodexConstants;
    using static ViewUtilities;

    public partial class SourceFileRenderer
    {
        BoundSourceFile _sourceFile;
        private string projectId;

        public SourceFileViewModel ViewModel { get; }

        public SourceFileRenderer(SourceFileViewModel sourceView)
        {
            Contract.Requires(sourceView?.SourceFile != null);
            ViewModel = sourceView;

            _sourceFile = (BoundSourceFile)sourceView.SourceFile;
            this.projectId = _sourceFile.SourceFile.Info.ProjectId;
        }

        /// <summary>
        /// Gets the contents of the source file with <span> tags added around
        /// all the spans specified for this BoundSourceFile that have a
        /// class of the Classification for the Symbol
        /// </summary>
        /// <returns></returns>
        public EditorModel Render()
        {
            var filePath = _sourceFile.SourceFile?.Info?.ProjectRelativePath;

            var model = new EditorModel()
            {
                ProjectId = projectId,
                FilePath = filePath,
                WebLink = _sourceFile.SourceFile?.Info?.WebAddress,
                DownloadLink = _sourceFile.SourceFile?.Info?.DownloadAddress,
                RepoRelativePath = GetEffectiveRepoRelativePath(),
                ProjectRelativePath = _sourceFile.SourceFile?.Info?.ProjectRelativePath,
                IndexedOn = _sourceFile.Commit?.DateUploaded.ToLocalTime().ToString() ?? "Unknown",
                RepoName = _sourceFile.SourceFile?.Info?.RepositoryName ?? "Unknown",
                OpenFileLink = ViewModel.OpenFileLink ?? ViewModelAddress.GoToFile(projectId, filePath)
            };

            model.IndexName = $"{model.RepoName} (commit: {_sourceFile.Commit?.CommitId})";
            int lineCount = _sourceFile.SourceFile.Info.Lines;
            var address = ViewModelAddress.GoToSpan(projectId, filePath, lineNumber: null);
            var url = address.ToString();
            model.LineNumberText = GenerateLineNumberText(lineCount, url);
            var ret = new StringBuilder();

            using (var sw = new StringWriter(ret))
            {
                ViewModel.Append(new HtmlSourceFileRenderer(this, sw));

                foreach (var toolbarButton in GetToolbarButtons())
                {
                    WriteHtmlElement(sw, toolbarButton, string.Empty);
                }
            }

            model.Text = ret.ToString();
            return model;
        }

        public record HtmlSourceFileRenderer(SourceFileRenderer Owner, StringWriter Writer) : ISourceFileRenderer
        {
            public void Append(SourceSpan span, StringSpan text)
            {
                var element = Owner.GetElementForSpan(span);
                Owner.WriteHtmlElement(Writer, element, text);
            }

            public void AppendRaw(StringSpan text)
            {
                Writer.Write(Html(text));
            }
        }

        private IEnumerable<BaseElementInfo> GetToolbarButtons()
        {
            yield return new HtmlElementInfo()
            {
                Name = "img",
                ["src"] = "content/icons/documentoutline.png",
                ["title"] = "Document Outline",
                CssClass = "documentOutlineButton",
                Link = ViewModelAddress.ShowDocumentOutline(_sourceFile.ProjectId, _sourceFile.ProjectRelativePath),
            };

            yield return new HtmlElementInfo()
            {
                Name = "img",
                ["src"] = "content/icons/csharpprojectexplorer.png",
                ["title"] = "Project Explorer",
                CssClass = "projectExplorerButton",
                Link = ViewModelAddress.ShowProjectExplorer(_sourceFile.ProjectId),
            };

            yield return new HtmlElementInfo()
            {
                Name = "img",
                ["src"] = "content/icons/namespaceexplorer.png",
                ["title"] = "Namespace Explorer",
                CssClass = "namespaceExplorerButton",
                Link = ViewModelAddress.ShowNamespaceExplorer(_sourceFile.ProjectId),
            };

            yield break;
        }

        private string GetEffectiveRepoRelativePath()
        {
            var path = _sourceFile.SourceFile?.Info?.RepoRelativePath;
            if (path == null) return "";
            if (path.Contains("[") || path.StartsWith("\\"))
            {
                return "";
            }

            return path;
        }

        private static string GenerateLineNumberText(int lineNumbers, string documentUrl)
        {
            if (lineNumbers == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            for (int i = 1; i <= lineNumbers; i++)
            {
                var lineNumber = i.ToString();
                sb.Append(
                    $"<a id=\"l{lineNumber}\" href=\"{documentUrl}&line={lineNumber}\" onclick=\"CxNav(this);return false;\">{lineNumber}</a><br/>"
                );
            }

            return sb.ToString();
        }

        public BaseElementInfo GetElementForSpan(SourceSpan span)
        {
            BaseElementInfo info;
            if (span.Reference != null)
            {
                var linkInfo = GenerateHyperlinkForReference(span, isUnclassified: span.Classification == null);
                ViewModel.ReferenceHtml?.Add(linkInfo);
                info = linkInfo;
            }
            else
            {
                info = new BaseElementInfo();
            }

            ViewModel.AllHtml?.Add(info);

            if (span.Classification is { } cspan)
            {
                var cssClass = MapClassificationToCssClass(cspan.Classification.StringValue);

                if (cspan.LocalGroupId > 0)
                {
                    info.SetClick("t(this);", set: false);
                    var referenceClass = $"r{cspan.LocalGroupId} r";
                    cssClass = string.IsNullOrEmpty(cssClass) ? referenceClass : $"{referenceClass} {cssClass}";
                }

                if (info.RequiresWrappingSpan && info.CssClass != null)
                {
                    info.OuterSpanClass = cssClass;
                }
                else
                {
                    info.AppendClass(cssClass);
                }
            }

            return info;
        }

        void WriteHtmlElement(TextWriter tw, BaseElementInfo htmlElementInfo, StringSpan innerText)
        {
            htmlElementInfo.Write(tw, innerText);
        }

        HtmlElementInfo GenerateHyperlinkForReference(SourceSpan sourceSpan, bool isUnclassified)
        {
            var span = sourceSpan.Reference;
            var symbol = span.Reference;
            string idHash = symbol.Id.Value;

            bool isMsBuild = _sourceFile.SourceFile.Info.Language == "msbuild";
            var address = ViewModelAddress.ForSymbol(symbol, referencingProjectId: projectId, out var isDistributedDefinition, out var isDefinition, out var isProjectScopedReference);
            if (!isDefinition)
            {
                idHash = null;
            }

            var result = new HtmlElementInfo()
            {
                Name = "a",
                CssClass = isDistributedDefinition || isUnclassified ? "msbuildlink" : null,
                DeclaredSymbolId = symbol.Id.Value,
                Symbol = symbol,
                SourceSpan = sourceSpan,
                RequiresWrappingSpan = isMsBuild,
                Span = span,
                Link = address,
            };

            result.AppendClass(idHash);
            result.AppendClass($"l{span.LineNumber}");

            return result;
        }

        private static HashSet<StringEnum<ClassificationName>> ignoreClassifications = new(new StringEnum<ClassificationName>[]
            {
                "operator",
                "number",
                "punctuation",
                "preprocessor text",
                "xml literal - text",
                "xml - text",
                ClassificationName.Operator,
                ClassificationName.Number,
                ClassificationName.Punctuation,
                ClassificationName.PreprocessorText,
                ClassificationName.XmlLiteralText,
                ClassificationName.XmlText,

            });

        private static Dictionary<StringEnum<ClassificationName>, string> replaceClassifications = new()
            {
                { ClassificationName.XmlDelimiter, Constants.ClassificationXmlDelimiter },
                { ClassificationName.XmlName, Constants.ClassificationXmlName },
                { ClassificationName.XmlAttributeName, Constants.ClassificationXmlAttributeName },
                { ClassificationName.XmlAttributeQuotes, Constants.ClassificationXmlAttributeQuotes },
                { ClassificationName.XmlAttributeValue, Constants.ClassificationXmlAttributeValue },
                { ClassificationName.XmlEntityReference, Constants.ClassificationXmlEntityReference },
                { ClassificationName.XmlCdataSection, Constants.ClassificationXmlCDataSection },
                { ClassificationName.XmlProcessingInstruction, Constants.ClassificationXmlProcessingInstruction },
                { ClassificationName.XmlComment, Constants.ClassificationComment },

                { ClassificationName.Keyword, Constants.ClassificationKeyword },
                { ClassificationName.KeywordControl, Constants.ClassificationKeyword },
                { ClassificationName.Identifier, Constants.ClassificationIdentifier },
                { ClassificationName.ClassName, Constants.ClassificationTypeName },
                { ClassificationName.StructName, Constants.ClassificationTypeName },
                { ClassificationName.InterfaceName, Constants.ClassificationTypeName },
                { ClassificationName.EnumName, Constants.ClassificationTypeName },
                { ClassificationName.DelegateName, Constants.ClassificationTypeName },
                { ClassificationName.ModuleName, Constants.ClassificationTypeName },
                { ClassificationName.RecordClassName, Constants.ClassificationTypeName },
                { ClassificationName.RecordStructName, Constants.ClassificationTypeName },
                { ClassificationName.TypeParameterName, Constants.ClassificationTypeName },
                { ClassificationName.PreprocessorKeyword, Constants.ClassificationKeyword },
                { ClassificationName.XmlDocCommentDelimiter, Constants.ClassificationComment },
                { ClassificationName.XmlDocCommentName, Constants.ClassificationComment },
                { ClassificationName.XmlDocCommentText, Constants.ClassificationComment },
                { ClassificationName.XmlDocCommentComment, Constants.ClassificationComment },
                { ClassificationName.XmlDocCommentEntityReference, Constants.ClassificationComment },
                { ClassificationName.XmlDocCommentAttributeName, Constants.ClassificationComment },
                { ClassificationName.XmlDocCommentAttributeQuotes, Constants.ClassificationComment },
                { ClassificationName.XmlDocCommentAttributeValue, Constants.ClassificationComment },
                { ClassificationName.XmlDocCommentCdataSection, Constants.ClassificationComment },
                { ClassificationName.XmlLiteralDelimiter, Constants.ClassificationXmlLiteralDelimiter },
                { ClassificationName.XmlLiteralName, Constants.ClassificationXmlLiteralName },
                { ClassificationName.XmlLiteralAttributeName, Constants.ClassificationXmlLiteralAttributeName },
                { ClassificationName.XmlLiteralAttributeQuotes, Constants.ClassificationXmlLiteralAttributeQuotes },
                { ClassificationName.XmlLiteralAttributeValue, Constants.ClassificationXmlLiteralAttributeValue },
                { ClassificationName.XmlLiteralEntityReference, Constants.ClassificationXmlLiteralEntityReference },
                { ClassificationName.XmlLiteralCdataSection, Constants.ClassificationXmlLiteralCDataSection },
                { ClassificationName.XmlLiteralProcessingInstruction, Constants.ClassificationXmlLiteralProcessingInstruction },
                { ClassificationName.XmlLiteralEmbeddedExpression, Constants.ClassificationXmlLiteralEmbeddedExpression },
                { ClassificationName.XmlLiteralComment, Constants.ClassificationComment },
                { ClassificationName.Comment, Constants.ClassificationComment },
                { ClassificationName.String, Constants.ClassificationLiteral },
                { ClassificationName.StringVerbatim, Constants.ClassificationLiteral },
                { ClassificationName.ExcludedCode, Constants.ClassificationExcludedCode },
            };

        public static string MapClassificationToCssClass(StringEnum<ClassificationName> classificationType)
        {
            if (string.IsNullOrEmpty(classificationType.StringValue) || ignoreClassifications.Contains(classificationType))
            {
                return null;
            }

            if (classificationType == Constants.ClassificationKeyword)
            {
                return classificationType.StringValue;
            }

            string replacement = null;
            if (!replaceClassifications.TryGetValue(classificationType, out replacement))
            {
                return null;
            }

            if (replacement == null ||
                replacement == "" ||
                replacement == Constants.ClassificationIdentifier ||
                replacement == Constants.ClassificationPunctuation)
            {
                // identifiers are conveniently black by default so let's save some space
                return null;
            }

            return replacement;
        }

        public class Constants
        {
            //public static readonly string IDResolvingFileName = "A";
            //public static readonly string PartialResolvingFileName = "P";
            //public static readonly string ReferencesFileName = "R";
            //public static readonly string DeclaredSymbolsFileName = "D";
            //public static readonly string MasterIndexFileName = "DeclaredSymbols.txt";
            //public static readonly string ReferencedAssemblyList = "References";
            //public static readonly string UsedReferencedAssemblyList = "UsedReferences";
            //public static readonly string ReferencingAssemblyList = "ReferencingAssemblies";
            //public static readonly string ProjectInfoFileName = "i";
            //public static readonly string MasterProjectMap = "Projects";
            //public static readonly string MasterAssemblyMap = "Assemblies";
            //public static readonly string Namespaces = "namespaces.html";

            public static readonly string ClassificationIdentifier = "i";
            public static readonly string ClassificationKeyword = "k";
            public static readonly string ClassificationTypeName = "t";
            public static readonly string ClassificationComment = "c";
            public static readonly string ClassificationLiteral = "s";

            public static readonly string ClassificationXmlDelimiter = "xd";
            public static readonly string ClassificationXmlName = "xn";
            public static readonly string ClassificationXmlAttributeName = "xan";
            public static readonly string ClassificationXmlAttributeValue = "xav";
            public static readonly string ClassificationXmlAttributeQuotes = null;
            public static readonly string ClassificationXmlEntityReference = "xer";
            public static readonly string ClassificationXmlCDataSection = "xcs";
            public static readonly string ClassificationXmlProcessingInstruction = "xpi";

            public static readonly string ClassificationXmlLiteralDelimiter = "xld";
            public static readonly string ClassificationXmlLiteralName = "xln";
            public static readonly string ClassificationXmlLiteralAttributeName = "xlan";
            public static readonly string ClassificationXmlLiteralAttributeValue = "xlav";
            public static readonly string ClassificationXmlLiteralAttributeQuotes = "xlaq";
            public static readonly string ClassificationXmlLiteralEntityReference = "xler";
            public static readonly string ClassificationXmlLiteralCDataSection = "xlcs";
            public static readonly string ClassificationXmlLiteralEmbeddedExpression = "xlee";
            public static readonly string ClassificationXmlLiteralProcessingInstruction = "xlpi";

            public static readonly string ClassificationExcludedCode = "e";
            //public static readonly string RoslynClassificationKeyword = "keyword";
            //public static readonly string DeclarationMap = "DeclarationMap";
            public static readonly string ClassificationPunctuation = "punctuation";
            //public static readonly string ProjectExplorer = "ProjectExplorer";
            //public static readonly string SolutionExplorer = "SolutionExplorer";
            //public static readonly string HuffmanFileName = "Huffman.txt";
            //public static readonly string TopReferencedAssemblies = "TopReferencedAssemblies";
            //public static readonly string BaseMembersFileName = "BaseMembers";
            //public static readonly string ImplementedInterfaceMembersFileName = "ImplementedInterfaceMembers";
            //public static readonly string GuidAssembly = "GuidAssembly";
            //public static readonly string MSBuildPropertiesAssembly = "MSBuildProperties";
            //public static readonly string MSBuildItemsAssembly = "MSBuildItems";
            //public static readonly string MSBuildTargetsAssembly = "MSBuildTargets";
            //public static readonly string MSBuildTasksAssembly = "MSBuildTasks";
            //public static readonly string MSBuildFiles = "MSBuildFiles";
            //public static readonly string TypeScriptFiles = "TypeScriptFiles";
            //public static readonly string AssemblyPaths = @"AssemblyPaths.txt";
        }
    }
}