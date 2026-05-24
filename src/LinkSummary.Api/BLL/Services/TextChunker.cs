using LinkSummary.Api.BLL.Abstract;
using System.Text;

namespace LinkSummary.Api.BLL.Services
{
    public class TextChunker : ITextChunker
    {
        private const int SingleChunkThreshold = 21000;
        private const int TargetChunkSize = 15000;

        public List<string> Chunk(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string> { text ?? string.Empty };

            if (text.Length <= SingleChunkThreshold)
                return new List<string> { text };

            var numChunks = (int)Math.Ceiling((double)text.Length / TargetChunkSize);
            var targetChunkSize = (int)Math.Ceiling((double)text.Length / numChunks);

            var paragraphs = text.Split(new[] { "\n\n" }, StringSplitOptions.None);
            var chunks = new List<string>(numChunks);
            var currentChunk = new StringBuilder();
            var currentLength = 0;

            foreach (var paragraph in paragraphs)
            {
                var paragraphLength = paragraph.Length;
                var newLength = currentLength + (currentLength > 0 ? 2 : 0) + paragraphLength;

                if (currentLength > 0 && newLength > targetChunkSize && chunks.Count < numChunks - 1)
                {
                    chunks.Add(currentChunk.ToString());
                    currentChunk.Clear();
                    currentLength = 0;
                }

                if (currentLength > 0)
                {
                    currentChunk.Append("\n\n");
                    currentLength += 2;
                }

                currentChunk.Append(paragraph);
                currentLength += paragraphLength;
            }

            if (currentLength > 0)
                chunks.Add(currentChunk.ToString());

            return chunks;
        }
    }
}
