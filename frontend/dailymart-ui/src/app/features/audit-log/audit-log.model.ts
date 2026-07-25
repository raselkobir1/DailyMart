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

/** Mirrors the backend's AuditAction enum (Domain/Auditing/AuditAction.cs) - sent to the API as the
 * string name (System.Text.Json's default enum serialization), not the numeric ordinal. */
export const AUDIT_ACTIONS = ['Created', 'Updated', 'Deleted', 'Sold'] as const;

export interface AuditLogFilter {
  entityName?: string | null;
  action?: string | null;
  fromDate?: string | null;
  toDate?: string | null;
  searchTerm?: string | null;
}
