import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { SupplierDto } from '../supplier.model';
import { SupplierService } from '../supplier.service';

/** Inline add/edit form, no MatDialog - same pattern as Module 3's master data pages. OpeningBalance is
 * only shown while creating - it's write-once server-side (Module 5 Step 1). */
@Component({
  selector: 'app-supplier-list',
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
  templateUrl: './supplier-list.component.html',
  styleUrl: './supplier-list.component.scss'
})
export class SupplierListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly supplierService = inject(SupplierService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly displayedColumns = ['name', 'contactPerson', 'phone', 'currentDue', 'actions'];
  protected readonly items = signal<SupplierDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly formVisible = signal(false);
  protected readonly editingId = signal<number | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    contactPerson: [''],
    phone: [''],
    email: ['', Validators.email],
    address: [''],
    openingBalance: [0]
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
    this.form.reset({ name: '', contactPerson: '', phone: '', email: '', address: '', openingBalance: 0 });
    this.formVisible.set(true);
  }

  protected startEdit(supplier: SupplierDto): void {
    this.editingId.set(supplier.id);
    this.form.reset({
      name: supplier.name,
      contactPerson: supplier.contactPerson ?? '',
      phone: supplier.phone ?? '',
      email: supplier.email ?? '',
      address: supplier.address ?? '',
      openingBalance: supplier.openingBalance
    });
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
    const request = {
      name: raw.name,
      contactPerson: raw.contactPerson || null,
      phone: raw.phone || null,
      email: raw.email || null,
      address: raw.address || null
    };

    this.saving.set(true);
    const id = this.editingId();
    const result$ = id === null
      ? this.supplierService.create({ ...request, openingBalance: raw.openingBalance })
      : this.supplierService.update(id, request);

    result$.subscribe({
      next: () => {
        this.saving.set(false);
        this.formVisible.set(false);
        this.snackBar.open('Supplier saved.', 'Dismiss', { duration: 3000 });
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.snackBar.open(error.error?.title ?? 'Could not save supplier.', 'Dismiss');
      }
    });
  }

  protected delete(supplier: SupplierDto): void {
    if (!confirm(`Delete supplier "${supplier.name}"?`)) {
      return;
    }

    this.supplierService.delete(supplier.id).subscribe({
      next: () => {
        this.snackBar.open('Supplier deleted.', 'Dismiss', { duration: 3000 });
        this.load();
      },
      error: () => this.snackBar.open('Could not delete supplier.', 'Dismiss')
    });
  }

  protected viewLedger(supplier: SupplierDto): void {
    this.router.navigateByUrl(`/suppliers/${supplier.id}/ledger`);
  }

  private load(): void {
    this.loading.set(true);

    this.supplierService.getPaged({ pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Could not load suppliers.', 'Dismiss');
      }
    });
  }
}
