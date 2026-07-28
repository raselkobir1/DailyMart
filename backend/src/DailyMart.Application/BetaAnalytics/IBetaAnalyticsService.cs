namespace DailyMart.Application.BetaAnalytics;

/// <summary>Demo service backing the "Beta Analytics" module - see BetaAnalyticsController's doc
/// comment for why this exists.</summary>
public interface IBetaAnalyticsService
{
    Task<BetaAnalyticsSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
