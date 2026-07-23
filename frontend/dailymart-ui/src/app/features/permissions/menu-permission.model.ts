export interface MenuPermissionResponse {
  menuId: number;
  menuKey: string;
  label: string;
  route: string;
  icon: string;
  sortOrder: number;
  parentId: number | null;
  canView: boolean;
  canCreate: boolean;
  canEdit: boolean;
  canDelete: boolean;
}

export interface MenuPermissionItem {
  menuId: number;
  canView: boolean;
  canCreate: boolean;
  canEdit: boolean;
  canDelete: boolean;
}

export interface SetPermissionsRequest {
  permissions: MenuPermissionItem[];
}
