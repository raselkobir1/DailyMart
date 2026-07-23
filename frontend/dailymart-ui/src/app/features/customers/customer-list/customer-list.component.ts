import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Perms } from '../../../core/perms';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { CustomerDto } from '../customer.model';
import { CustomerService } from '../customer.service';

/** Inline add/edit form, no modal - same pattern as Modules 3/5. currentDue/Ledger were added in
 * Module 9 (POS Sales) once a customer could actually accrue due via a credit sale. */
@Component({
  selector: 'app-customer-list',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, PaginationComponent],
  templateUrl: './customer-list.component.html',
  styleUrl: './customer-list.component.scss'
})
export class CustomerListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly customerService = inject(CustomerService);
  private readonly router = inject(Router);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

  protected readonly items = signal<CustomerDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly formVisible = signal(false);
  protected readonly editingId = signal<number | null>(null);
  protected searchTerm = '';

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    phone: [''],
    email: ['', Validators.email],
    address: ['']
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

  protected search(): void {
    this.pageNumber.set(1);
    this.load();
  }

  protected startCreate(): void {
    this.editingId.set(null);
    this.form.reset({ name: '', phone: '', email: '', address: '' });
    this.formVisible.set(true);
  }

  protected startEdit(customer: CustomerDto): void {
    this.editingId.set(customer.id);
    this.form.reset({
      name: customer.name,
      phone: customer.phone ?? '',
      email: customer.email ?? '',
      address: customer.address ?? ''
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
      phone: raw.phone || null,
      email: raw.email || null,
      address: raw.address || null
    };

    this.saving.set(true);
    const id = this.editingId();
    const result$ = id === null ? this.customerService.create(request) : this.customerService.update(id, request);

    result$.subscribe({
      next: () => {
        this.saving.set(false);
        this.formVisible.set(false);
        this.toast.success('Customer saved.');
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not save customer.');
      }
    });
  }

  protected delete(customer: CustomerDto): void {
    if (!confirm(`Delete customer "${customer.name}"?`)) {
      return;
    }

    this.customerService.delete(customer.id).subscribe({
      next: () => {
        this.toast.success('Customer deleted.');
        this.load();
      },
      error: () => this.toast.error('Could not delete customer.')
    });
  }

  protected viewLedger(customer: CustomerDto): void {
    this.router.navigateByUrl(`/customers/${customer.id}/ledger`);
  }

  protected viewDueReport(): void {
    this.router.navigateByUrl('/customers/due-report');
  }

  private load(): void {
    this.loading.set(true);

    this.customerService
      .getPaged({ pageNumber: this.pageNumber(), pageSize: this.pageSize(), searchTerm: this.searchTerm || undefined })
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.toast.error('Could not load customers.');
        }
      });
  }
}
