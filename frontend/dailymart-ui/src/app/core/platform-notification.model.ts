/** Mirrors the backend's PlatformNotificationDto - see IPlatformNotificationStore's doc comment. */
export interface PlatformNotificationDto {
  id: number;
  type: string;
  tenantId: number | null;
  tenantName: string;
  adminUsername: string | null;
  isRead: boolean;
  createdAt: string;
}
