import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Perms } from '../../../core/perms';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { RoleDto } from '../role.model';
import { RoleService } from '../role.service';

@Component({
  selector: 'app-role-list',
  standalone: true,
  imports: [ReactiveFormsModule, PaginationComponent],
  templateUrl: './role-list.component.html',
  styleUrl: './role-list.component.scss'
})
export class RoleListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly roleService = inject(RoleService);
  private readonly toast = inject(Toast);
  private readonly router = inject(Router);
  protected readonly perms = inject(Perms);

  protected readonly items = signal<RoleDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly formVisible = signal(false);
  protected readonly editingId = signal<number | null>(null);
  protected readonly editingIsSystem = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['']
  });

  ngOnInit(): void {
    this.load();
  }

  protected onPageChange(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
    this.load();
  }

  protected onPageSizeChange(pageSize: number): void {
    this.pageSize.set(pageSize);
    this.pageNumber.set(1);
    this.load();
  }

  protected startCreate(): void {
    this.editingId.set(null);
    this.editingIsSystem.set(false);
    this.form.reset({ name: '', description: '' });
    this.form.controls.name.enable();
    this.formVisible.set(true);
  }

  protected startEdit(role: RoleDto): void {
    this.editingId.set(role.id);
    this.editingIsSystem.set(role.isSystem);
    this.form.reset({ name: role.name, description: role.description ?? '' });
    if (role.isSystem) {
      this.form.controls.name.disable();
    } else {
      this.form.controls.name.enable();
    }
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

    const request = { name: raw.name, description: raw.description || null };
    const result$ = id === null ? this.roleService.create(request) : this.roleService.update(id, request);

    result$.subscribe({
      next: () => {
        this.saving.set(false);
        this.formVisible.set(false);
        this.toast.success('Role saved.');
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not save role.');
      }
    });
  }

  protected delete(role: RoleDto): void {
    if (!confirm(`Delete role "${role.name}"?`)) {
      return;
    }

    this.roleService.delete(role.id).subscribe({
      next: () => {
        this.toast.success('Role deleted.');
        this.load();
      },
      error: (error) => this.toast.error(error.error?.title ?? 'Could not delete role.')
    });
  }

  protected managePermissions(role: RoleDto): void {
    this.router.navigateByUrl(`/permissions?roleId=${role.id}`);
  }

  private load(): void {
    this.loading.set(true);

    this.roleService.getPaged({ pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load roles.');
      }
    });
  }
}
