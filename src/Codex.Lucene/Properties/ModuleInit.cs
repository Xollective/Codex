using System.Runtime.CompilerServices;
using Codex;
using Lucene.Net.Codecs;

public class ModuleInit
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Tie ChecksumEnabled to corresponding SdkFeature
        CodecUtil.ChecksumEnabled = () => Features.EnableIndexChecksum;
    }
}