namespace Tenray.ZoneTree.AbstractFileStream;

public class StreamFileStream(string filePath, Stream stream) : IStreamFileStream
{
    public Stream Stream => stream;

    public string FilePath => filePath;

    public void Flush(bool flushToDisk)
    {
        if (Stream is FileStream fs)
        {
            fs.Flush(flushToDisk);
        }
        else
        {
            Stream.Flush();
        }
    }
}
