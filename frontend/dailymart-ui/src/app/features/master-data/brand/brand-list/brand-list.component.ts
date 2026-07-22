import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { BrandDto } from '../brand.model';
import { BrandService } from '../brand.service';

@Component({
  selector: 'app-brand-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './brand-list.component.html',
  styleUrl: './brand-list.component.scss'
})
export class BrandListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly brandService = inject(BrandService);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly displayedColumns = ['name', 'description', 'actions'];
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

  protected onPageChange(event: PageEvent): void {
    this.pageNumber.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
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
        this.snackBar.open('Brand saved.', 'Dismiss', { duration: 3000 });
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.snackBar.open(error.error?.title ?? 'Could not save brand.', 'Dismiss');
      }
    });
  }

  protected delete(brand: BrandDto): void {
    if (!confirm(`Delete brand "${brand.name}"?`)) {
      return;
    }

    this.brandService.delete(brand.id).subscribe({
      next: () => {
        this.snackBar.open('Brand deleted.', 'Dismiss', { duration: 3000 });
        this.load();
      },
      error: () => this.snackBar.open('Could not delete brand.', 'Dismiss')
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
        this.snackBar.open('Could not load brands.', 'Dismiss');
      }
    });
  }
}
