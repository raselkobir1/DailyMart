/** Mirrors the backend's SupportMessageDto - see ISupportChatService's doc comment. */
export interface SupportMessageDto {
  id: number;
  tenantId: number;
  fromPlatformAdmin: boolean;
  senderName: string;
  message: string;
  createdAt: string;
}
