/** Mirrors the backend's PlanDto, as returned by the public (unauthenticated) GET /public/plans - only
 * the fields a marketing pricing section needs, nothing tenant-specific. */
export interface LandingPlanDto {
  id: number;
  name: string;
  description: string | null;
  price: number;
  billingCycle: string;
  isFree: boolean;
  sortOrder: number;
}
