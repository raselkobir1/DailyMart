import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Toast } from '../../../core/toast';
import { MenuDto } from '../menu.model';
import { MenuService } from '../menu.service';

interface FlatMenuRow extends MenuDto {
  depth: number;
}

/** Flattens the parent/child tree into an indented list (↳ prefix per level) - same display approach as
 * the reference app's Menus screen. Unpaginated, see MenuService's doc comment. */
function flatten(menus: MenuDto[]): FlatMenuRow[] {
  const byParent = new Map<number | null, MenuDto[]>();
  for (const menu of menus) {
    const key = menu.parentId ?? null;
    const list = byParent.get(key) ?? [];
    list.push(menu);
    byParent.set(key, list);
  }

  const rows: FlatMenuRow[] = [];
  const visit = (parentId: number | null, depth: number) => {
    for (const menu of (byParent.get(parentId) ?? []).sort((a, b) => a.sortOrder - b.sortOrder)) {
      rows.push({ ...menu, depth });
      visit(menu.id, depth + 1);
    }
  };
  visit(null, 0);
  return rows;
}

/** Read-only - Menu is a shared/global table every tenant reads (CLAUDE.md §4), so a tenant's own Admin
 * has no authority to create/edit/delete rows here (MenusController now requires the platform-admin-only
 * policy for those); this screen exists so an Admin can still see the available menu keys/routes while
 * working with the Permissions matrix. */
@Component({
  selector: 'app-menu-list',
  standalone: true,
  imports: [],
  templateUrl: './menu-list.component.html',
  styleUrl: './menu-list.component.scss'
})
export class MenuListComponent implements OnInit {
  private readonly menuService = inject(MenuService);
  private readonly toast = inject(Toast);

  protected readonly menus = signal<MenuDto[]>([]);
  protected readonly loading = signal(false);

  protected readonly rows = computed(() => flatten(this.menus()));

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);

    this.menuService.getAll().subscribe({
      next: (menus) => {
        this.menus.set(menus);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load menus.');
      }
    });
  }
}
