using LinkSummary.Api.BLL.Abstract;
using LinkSummary.Api.BLL.Exceptions;
using System.Reflection;

namespace LinkSummary.Api.BLL.Services
{
    public class PromptService : IPromptService
    {
        private const string SystemPromptResourceName = "LinkSummary.Api.BLL.Prompts.LinkSummarySystemPrompt.txt";
        private const string UserPromptResourceName = "LinkSummary.Api.BLL.Prompts.LinkSummaryUserPrompt.txt";

        private static readonly Lazy<string> SystemPromptLazy = new Lazy<string>(() =>
            LoadPromptFromResources(SystemPromptResourceName));

        private static readonly Lazy<string> UserPromptLazy = new Lazy<string>(() =>
            LoadPromptFromResources(UserPromptResourceName));

        private static string SystemPrompt => SystemPromptLazy.Value;

        private static string UserPrompt => UserPromptLazy.Value;

        public string GetSystemPrompt(int version)
        {
            if (version == 1) 
            {
                return SystemPrompt
                    .Replace("\r\n", " ")
                    .Replace("\n", " ")
                    .Replace("\r", " "); ;
            }

            throw new Exception("not found version for system prompt");
        }

        public string GetUserPrompt(int version)
        {
            if (version == 1)
            {
                return UserPrompt
                    .Replace("\r\n", " ")
                    .Replace("\n", " ")
                    .Replace("\r", " ");
            }

            throw new Exception("not found version for system prompt");
        }

        private static string LoadPromptFromResources(string promptResourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(promptResourceName);
            if (stream == null)
                throw new ResourceNotFoundException(promptResourceName);

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
