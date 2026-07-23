export interface RoleDto {
  id: number;
  name: string;
  description: string | null;
  isSystem: boolean;
  isDefault: boolean;
}

/** Used for both create and update - IsSystem/IsDefault are never caller-settable. */
export interface RoleRequest {
  name: string;
  description: string | null;
}
