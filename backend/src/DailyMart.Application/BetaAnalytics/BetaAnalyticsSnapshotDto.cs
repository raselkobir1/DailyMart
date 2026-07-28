namespace DailyMart.Application.BetaAnalytics;

/// <summary>Demo payload for the "Beta Analytics" module - see BetaAnalyticsController's doc comment.
/// Content is intentionally trivial; this module exists to demonstrate IFeatureEntitlementService end
/// to end, not to ship a real analytics feature.</summary>
public class BetaAnalyticsSnapshotDto
{
    public string Headline { get; init; } = string.Empty;

    public int SignalScore { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }
}
