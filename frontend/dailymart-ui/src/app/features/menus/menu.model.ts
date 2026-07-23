export interface MenuDto {
  id: number;
  key: string;
  label: string;
  route: string;
  icon: string;
  sortOrder: number;
  parentId: number | null;
}

/** The update shape - no key (write-once). CreateMenuRequest extends it with the one field only
 * creation can set. */
export interface MenuRequest {
  label: string;
  route: string;
  icon: string;
  sortOrder: number;
  parentId: number | null;
}

export interface CreateMenuRequest extends MenuRequest {
  key: string;
}
