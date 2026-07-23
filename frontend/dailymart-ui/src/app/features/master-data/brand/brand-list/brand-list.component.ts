import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Perms } from '../../../../core/perms';
import { Toast } from '../../../../core/toast';
import { PaginationComponent } from '../../../../shared/pagination/pagination.component';
import { BrandDto } from '../brand.model';
import { BrandService } from '../brand.service';

@Component({
  selector: 'app-brand-list',
  standalone: true,
  imports: [ReactiveFormsModule, PaginationComponent],
  templateUrl: './brand-list.component.html',
  styleUrl: './brand-list.component.scss'
})
export class BrandListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly brandService = inject(BrandService);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

  protected readonly items = signal<BrandDto[]>([]);
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

  protected startEdit(brand: BrandDto): void {
    this.editingId.set(brand.id);
    this.form.reset({ name: brand.name, description: brand.description ?? '' });
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
    const result$ = id === null ? this.brandService.create(request) : this.brandService.update(id, request);

    result$.subscribe({
      next: () => {
        this.saving.set(false);
        this.formVisible.set(false);
        this.toast.success('Brand saved.');
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not save brand.');
      }
    });
  }

  protected delete(brand: BrandDto): void {
    if (!confirm(`Delete brand "${brand.name}"?`)) {
      return;
    }

    this.brandService.delete(brand.id).subscribe({
      next: () => {
        this.toast.success('Brand deleted.');
        this.load();
      },
      error: () => this.toast.error('Could not delete brand.')
    });
  }

  private load(): void {
    this.loading.set(true);

    this.brandService.getPaged({ pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load brands.');
      }
    });
  }
}
