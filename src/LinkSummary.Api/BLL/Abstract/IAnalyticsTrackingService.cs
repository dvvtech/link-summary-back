namespace LinkSummary.Api.BLL.Abstract
{
    public interface IAnalyticsTrackingService
    {
        Task TrackLinkSummaryVisitAsync(
            string link,
            string clientIp,
            string? userAgent,
            CancellationToken cancellationToken = default);
    }
}
