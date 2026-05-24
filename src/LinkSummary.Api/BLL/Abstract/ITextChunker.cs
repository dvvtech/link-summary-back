namespace LinkSummary.Api.BLL.Abstract
{
    public interface ITextChunker
    {
        List<string> Chunk(string text);
    }
}
