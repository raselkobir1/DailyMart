export interface MenuPermission {
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
