/** Mirrors the backend's BetaAnalyticsSnapshotDto - see BetaAnalyticsController's doc comment. This
 * whole module is a demo of the per-tenant feature entitlement mechanism, not a real feature. */
export interface BetaAnalyticsSnapshotDto {
  headline: string;
  signalScore: number;
  generatedAt: string;
}
