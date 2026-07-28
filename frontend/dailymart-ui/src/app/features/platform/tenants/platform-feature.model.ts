/** Mirrors the backend's TenantMenuAvailabilityDto - one row per current Menu, denormalized with
 * whether this tenant can see it and why. See IFeatureEntitlementService's doc comment. */
export interface TenantMenuAvailabilityDto {
  menuId: number;
  menuKey: string;
  label: string;
  parentId: number | null;
  sortOrder: number;
  isGenerallyAvailable: boolean;
  isGranted: boolean;
  isAvailable: boolean;
}
