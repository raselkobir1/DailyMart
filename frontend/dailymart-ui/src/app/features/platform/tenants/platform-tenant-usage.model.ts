export interface TenantUsageSnapshotDto {
  tenantId: number;
  totalUsers: number;
  activeUsers: number;
  lastLoginAt: string | null;
  lastActivityAt: string | null;
  productCount: number;
  customerCount: number;
  supplierCount: number;
  saleCount: number;
  purchaseCount: number;
}
