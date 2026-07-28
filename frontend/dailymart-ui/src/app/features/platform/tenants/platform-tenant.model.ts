export interface PlatformTenantDto {
  id: number;
  name: string;
  isActive: boolean;
  createdAt: string;
  planName: string | null;
  isFree: boolean;
  currentPeriodEnd: string | null;
  isOverdue: boolean;
  totalUsers: number;
  activeUsers: number;
  lastLoginAt: string | null;
  lastActivityAt: string | null;
  lastActiveAt: string | null;
  unreadSupportMessages: number;
}
