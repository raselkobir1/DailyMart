import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Perms } from '../../../../core/perms';
import { Toast } from '../../../../core/toast';
import { PaginationComponent } from '../../../../shared/pagination/pagination.component';
import { CategoryDto } from '../category.model';
import { CategoryService } from '../category.service';

/**
 * Simple table + inline add/edit form, no modal - matches the login/settings pages' pattern of one
 * component per screen rather than a separate dialog component, and keeps this MVP CRUD screen small.
 * Delete confirmation uses the browser's confirm() rather than a custom dialog, for the same reason.
 */
@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [ReactiveFormsModule, PaginationComponent],
  templateUrl: './category-list.component.html',
  styleUrl: './category-list.component.scss'
})
export class CategoryListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly categoryService = inject(CategoryService);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

  protected readonly items = signal<CategoryDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly formVisible = signal(false);
  protected readonly editingId = signal<number | null>(null);

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
    this.form.reset({ name: '', description: '' });
    this.formVisible.set(true);
  }

  protected startEdit(category: CategoryDto): void {
    this.editingId.set(category.id);
    this.form.reset({ name: category.name, description: category.description ?? '' });
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

    const request = {
      name: this.form.getRawValue().name,
      description: this.form.getRawValue().description || null
    };

    this.saving.set(true);
    const id = this.editingId();
    const result$ = id === null ? this.categoryService.create(request) : this.categoryService.update(id, request);

    result$.subscribe({
      next: () => {
        this.saving.set(false);
        this.formVisible.set(false);
        this.toast.success('Category saved.');
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not save category.');
      }
    });
  }

  protected delete(category: CategoryDto): void {
    if (!confirm(`Delete category "${category.name}"?`)) {
      return;
    }

    this.categoryService.delete(category.id).subscribe({
      next: () => {
        this.toast.success('Category deleted.');
        this.load();
      },
      error: () => this.toast.error('Could not delete category.')
    });
  }

  private load(): void {
    this.loading.set(true);

    this.categoryService.getPaged({ pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load categories.');
      }
    });
  }
}
