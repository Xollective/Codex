using Codex.Lucene.Framework.AutoPrefix;
using Codex.ObjectModel;
using Codex.ObjectModel.Attributes;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene46;

namespace Codex.Lucene.Framework
{
    public class FieldMappingCodec : Lucene46Codec
    {
        static FieldMappingCodec()
        {
            PostingsFormat.SetPostingsFormatFactory(new PostingsFormatFactory());
        }

        public static void EnsureRegistered()
        {
            // Just calling this will trigger static constructor which will register factories
        }

        private readonly SearchType typeMapping;

        private AutoPrefixPostingsFormat AutoPrefixPostingsFormat { get; }

        public FieldMappingCodec(SearchType typeMapping)
        {
            this.typeMapping = typeMapping;

            AutoPrefixPostingsFormat = new AutoPrefixPostingsFormat();
        }

        public override DocValuesFormat GetDocValuesFormatForField(string field)
        {
            return base.GetDocValuesFormatForField(field);
        }

        public override PostingsFormat GetPostingsFormatForField(string field)
        {
            var fieldMapping = typeMapping[field];
            if (fieldMapping != null && 
                (fieldMapping.Behavior == SearchBehavior.PrefixShortName
                || fieldMapping.Behavior == SearchBehavior.PrefixFullName))
            {
                return AutoPrefixPostingsFormat;
            }

            return base.GetPostingsFormatForField(field);
        }

        private class PostingsFormatFactory : DefaultPostingsFormatFactory
        {
            protected override void Initialize()
            {
                PutPostingsFormatType(typeof(AutoPrefixPostingsFormat));
                base.Initialize();
            }
        }
    }

    //public class FieldMappingCodecFactory : ICodecFactory
    //{
    //    public static void Set()
    //    {
    //        Codec.SetCodecFactory(new FieldMappingCodecFactory());
    //    }

    //    public Codec GetCodec(string name)
    //    {
    //        return new FieldMappingCodec()
    //    }
    //}
}
