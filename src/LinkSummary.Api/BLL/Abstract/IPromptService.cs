namespace LinkSummary.Api.BLL.Abstract
{
    public interface IPromptService
    {
        string GetSystemPrompt(int version);

        string GetUserPrompt(int version);
    }
}
