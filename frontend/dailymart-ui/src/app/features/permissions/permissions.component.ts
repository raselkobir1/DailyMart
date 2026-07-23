import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Toast } from '../../core/toast';
import { RoleDto } from '../roles/role.model';
import { RoleService } from '../roles/role.service';
import { MenuPermissionResponse } from './menu-permission.model';
import { PermissionService } from './permission.service';

interface FlatPermissionRow extends MenuPermissionResponse {
  depth: number;
}

function flatten(items: MenuPermissionResponse[]): FlatPermissionRow[] {
  const byParent = new Map<number | null, MenuPermissionResponse[]>();
  for (const item of items) {
    const key = item.parentId ?? null;
    const list = byParent.get(key) ?? [];
    list.push(item);
    byParent.set(key, list);
  }

  const rows: FlatPermissionRow[] = [];
  const visit = (parentId: number | null, depth: number) => {
    for (const item of (byParent.get(parentId) ?? []).sort((a, b) => a.sortOrder - b.sortOrder)) {
      rows.push({ ...item, depth });
      visit(item.menuId, depth + 1);
    }
  };
  visit(null, 0);
  return rows;
}

/**
 * The permission-assignment screen: a single role dropdown drives a table of every menu (indented for
 * children) with 4 checkbox columns - View/Create/Edit/Delete - and one "Save changes" button that PUTs
 * the whole array back. Deliberately not a tree-with-checkboxes or a full role×menu grid all at once -
 * this exact "pick one role, edit its row in a flat table" shape is what was asked to be replicated.
 */
@Component({
  selector: 'app-permissions',
  standalone: true,
  templateUrl: './permissions.component.html',
  styleUrl: './permissions.component.scss'
})
export class PermissionsComponent implements OnInit {
  private readonly roleService = inject(RoleService);
  private readonly permissionService = inject(PermissionService);
  private readonly toast = inject(Toast);
  private readonly route = inject(ActivatedRoute);

  protected readonly roles = signal<RoleDto[]>([]);
  protected readonly selectedRoleId = signal<number | null>(null);
  protected readonly permissions = signal<MenuPermissionResponse[]>([]);
  protected readonly rows = computed(() => flatten(this.permissions()));
  protected readonly loadingRoles = signal(true);
  protected readonly loadingPermissions = signal(false);
  protected readonly saving = signal(false);

  ngOnInit(): void {
    const preselectRoleId = Number(this.route.snapshot.queryParamMap.get('roleId')) || null;

    this.roleService.getPaged({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: (result) => {
        this.roles.set(result.items);
        this.loadingRoles.set(false);

        const initial = preselectRoleId ?? result.items[0]?.id ?? null;
        if (initial !== null) {
          this.selectRole(initial);
        }
      },
      error: () => {
        this.loadingRoles.set(false);
        this.toast.error('Could not load roles.');
      }
    });
  }

  protected onRoleChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.selectRole(value);
  }

  protected toggle(row: FlatPermissionRow, field: 'canView' | 'canCreate' | 'canEdit' | 'canDelete'): void {
    this.permissions.update((list) =>
      list.map((item) => (item.menuId === row.menuId ? { ...item, [field]: !item[field] } : item))
    );
  }

  protected save(): void {
    const roleId = this.selectedRoleId();
    if (roleId === null) {
      return;
    }

    this.saving.set(true);
    const request = {
      permissions: this.permissions().map((p) => ({
        menuId: p.menuId,
        canView: p.canView,
        canCreate: p.canCreate,
        canEdit: p.canEdit,
        canDelete: p.canDelete
      }))
    };

    this.permissionService.setForRole(roleId, request).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success('Permissions saved.');
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not save permissions.');
      }
    });
  }

  private selectRole(roleId: number): void {
    this.selectedRoleId.set(roleId);
    this.loadingPermissions.set(true);

    this.permissionService.getForRole(roleId).subscribe({
      next: (permissions) => {
        this.permissions.set(permissions);
        this.loadingPermissions.set(false);
      },
      error: () => {
        this.loadingPermissions.set(false);
        this.toast.error('Could not load permissions for this role.');
      }
    });
  }
}
