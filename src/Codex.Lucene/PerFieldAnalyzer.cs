using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;

namespace Codex.Lucene.Search
{
    public class PerFieldAnalyzer : AnalyzerWrapper
    {
        private readonly SearchType typeMapping;
        private readonly SearchBehaviorAnalyzer searchBehaviorAnalyzer;
        private readonly Analyzer defaultAnalyzer;

        public PerFieldAnalyzer(SearchType typeMapping, Analyzer defaultAnalyzer)
            : base(PER_FIELD_REUSE_STRATEGY)
        {
            this.defaultAnalyzer = defaultAnalyzer;
            this.typeMapping = typeMapping;
            searchBehaviorAnalyzer = new SearchBehaviorAnalyzer(typeMapping);
        }

        public static Analyzer Create(SearchType typeMapping, Analyzer defaultAnalyzer)
        {
            Placeholder.Todo("Return per field analyzer after implementing search behaviors");
            return defaultAnalyzer;
        }

        protected override Analyzer GetWrappedAnalyzer(string fieldName)
        {
            var fieldMapping = typeMapping[fieldName];
            if (fieldMapping.Behavior == SearchBehavior.FullText || fieldMapping.Behavior == SearchBehavior.None)
            {
                return defaultAnalyzer;
            }
            else
            {
                return searchBehaviorAnalyzer;
            }
        }

        private class SearchBehaviorAnalyzer : Analyzer
        {
            private SearchType typeMapping;

            public SearchBehaviorAnalyzer(SearchType typeMapping)
            {
                this.typeMapping = typeMapping;
            }

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                var fieldMapping = typeMapping[fieldName];
                switch (fieldMapping.Behavior)
                {
                    case SearchBehavior.Term:
                        break;
                    case SearchBehavior.NormalizedKeyword:
                        break;
                    case SearchBehavior.Sortword:
                        break;
                    case SearchBehavior.PrefixFullName:
                        break;
                    case SearchBehavior.FullText:
                        break;
                    case SearchBehavior.PrefixTerm:
                        break;
                    case SearchBehavior.PrefixShortName:
                        break;
                }

                throw Placeholder.NotImplementedException();
            }
        }

        private class PrefixShortNameTokenizer : Tokenizer
        {
            public PrefixShortNameTokenizer(TextReader input) : base(input)
            {
            }

            public override bool IncrementToken()
            {
                throw new NotImplementedException();
            }
        }
    }
}
