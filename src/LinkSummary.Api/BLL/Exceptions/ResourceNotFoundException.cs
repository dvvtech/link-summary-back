namespace LinkSummary.Api.BLL.Exceptions
{
    public class ResourceNotFoundException : Exception
    {
        public ResourceNotFoundException(string resourceName, Exception? innerException = null)
            : base($"Resource '{resourceName}' not found", innerException)
        {
        }
    }
}
