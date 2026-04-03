namespace LinkSummary.Api.BLL.Abstract
{
    public interface IAnalyticsTrackingService
    {
        Task TrackVisitAsync(
            string link,
            string clientIp,
            string? userAgent,
            CancellationToken cancellationToken = default);
    }
}
