import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Perms } from '../../../core/perms';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { RoleDto } from '../../roles/role.model';
import { RoleService } from '../../roles/role.service';
import { UserDto } from '../user.model';
import { UserService } from '../user.service';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [ReactiveFormsModule, PaginationComponent],
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.scss'
})
export class UserListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);
  private readonly roleService = inject(RoleService);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

  protected readonly items = signal<UserDto[]>([]);
  protected readonly roles = signal<RoleDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly formVisible = signal(false);
  protected readonly editingId = signal<number | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.maxLength(100)]],
    password: ['', [Validators.minLength(8)]],
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    role: ['', Validators.required],
    isActive: [true]
  });

  ngOnInit(): void {
    this.roleService.getPaged({ pageNumber: 1, pageSize: 100 }).subscribe((result) => this.roles.set(result.items));
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
    this.form.reset({ username: '', password: '', fullName: '', role: '', isActive: true });
    this.form.controls.username.enable();
    this.form.controls.password.addValidators(Validators.required);
    this.formVisible.set(true);
  }

  protected startEdit(user: UserDto): void {
    this.editingId.set(user.id);
    this.form.reset({ username: user.username, password: '', fullName: user.fullName, role: user.role, isActive: user.isActive });
    this.form.controls.username.disable();
    this.form.controls.password.clearValidators();
    this.form.controls.password.updateValueAndValidity();
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
        ? this.userService.create({ username: raw.username, password: raw.password, fullName: raw.fullName, role: raw.role })
        : this.userService.update(id, { fullName: raw.fullName, role: raw.role, isActive: raw.isActive });

    result$.subscribe({
      next: () => {
        this.saving.set(false);
        this.formVisible.set(false);
        this.toast.success('User saved.');
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not save user.');
      }
    });
  }

  protected delete(user: UserDto): void {
    if (!confirm(`Delete user "${user.username}"?`)) {
      return;
    }

    this.userService.delete(user.id).subscribe({
      next: () => {
        this.toast.success('User deleted.');
        this.load();
      },
      error: (error) => this.toast.error(error.error?.title ?? 'Could not delete user.')
    });
  }

  private load(): void {
    this.loading.set(true);

    this.userService.getPaged({ pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load users.');
      }
    });
  }
}
