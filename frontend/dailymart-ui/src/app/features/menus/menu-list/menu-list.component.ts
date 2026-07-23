import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Perms } from '../../../core/perms';
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

@Component({
  selector: 'app-menu-list',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './menu-list.component.html',
  styleUrl: './menu-list.component.scss'
})
export class MenuListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly menuService = inject(MenuService);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

  protected readonly menus = signal<MenuDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly formVisible = signal(false);
  protected readonly editingId = signal<number | null>(null);

  protected readonly rows = computed(() => flatten(this.menus()));

  protected readonly form = this.fb.nonNullable.group({
    key: ['', [Validators.required, Validators.maxLength(100)]],
    label: ['', [Validators.required, Validators.maxLength(200)]],
    route: ['', [Validators.required, Validators.maxLength(200)]],
    icon: ['', [Validators.required, Validators.maxLength(20)]],
    sortOrder: [0, [Validators.min(0)]],
    parentId: [null as number | null]
  });

  /** Excludes self + all descendants, so a menu can never become its own ancestor (mirrors MenuService's
   * own server-side EnsureNoCycleAsync check). */
  protected parentOptions(): FlatMenuRow[] {
    const editingId = this.editingId();
    if (editingId === null) {
      return this.rows();
    }

    const excluded = new Set<number>([editingId]);
    let added = true;
    while (added) {
      added = false;
      for (const menu of this.menus()) {
        if (menu.parentId !== null && excluded.has(menu.parentId) && !excluded.has(menu.id)) {
          excluded.add(menu.id);
          added = true;
        }
      }
    }

    return this.rows().filter((m) => !excluded.has(m.id));
  }

  ngOnInit(): void {
    this.load();
  }

  protected startCreate(): void {
    this.editingId.set(null);
    this.form.reset({ key: '', label: '', route: '', icon: '', sortOrder: 0, parentId: null });
    this.form.controls.key.enable();
    this.formVisible.set(true);
  }

  protected startEdit(menu: MenuDto): void {
    this.editingId.set(menu.id);
    this.form.reset({ key: menu.key, label: menu.label, route: menu.route, icon: menu.icon, sortOrder: menu.sortOrder, parentId: menu.parentId });
    this.form.controls.key.disable();
    this.formVisible.set(true);
  }

  protected cancelEdit(): void {
    this.formVisible.set(false);
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const id = this.editingId();
    this.saving.set(true);

    const result$ =
      id === null
        ? this.menuService.create({ key: raw.key, label: raw.label, route: raw.route, icon: raw.icon, sortOrder: raw.sortOrder, parentId: raw.parentId })
        : this.menuService.update(id, { label: raw.label, route: raw.route, icon: raw.icon, sortOrder: raw.sortOrder, parentId: raw.parentId });

    result$.subscribe({
      next: () => {
        this.saving.set(false);
        this.formVisible.set(false);
        this.toast.success('Menu saved.');
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not save menu.');
      }
    });
  }

  protected delete(menu: MenuDto): void {
    if (!confirm(`Delete menu "${menu.label}"?`)) {
      return;
    }

    this.menuService.delete(menu.id).subscribe({
      next: () => {
        this.toast.success('Menu deleted.');
        this.load();
      },
      error: (error) => this.toast.error(error.error?.title ?? 'Could not delete menu.')
    });
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
