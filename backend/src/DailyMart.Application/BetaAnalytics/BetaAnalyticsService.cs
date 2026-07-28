namespace DailyMart.Application.BetaAnalytics;

public class BetaAnalyticsService : IBetaAnalyticsService
{
    public Task<BetaAnalyticsSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new BetaAnalyticsSnapshotDto
        {
            Headline = "You're in the Beta Analytics program.",
            SignalScore = 87,
            GeneratedAt = DateTimeOffset.UtcNow
        });
    }
}
