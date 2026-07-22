export interface AuditLogDto {
  id: number;
  entityName: string;
  entityId: string;
  action: string;
  oldValues: string | null;
  newValues: string | null;
  changedColumns: string | null;
  performedBy: string;
  performedAt: string;
}
