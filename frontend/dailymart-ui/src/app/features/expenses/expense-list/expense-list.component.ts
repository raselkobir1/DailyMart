import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Perms } from '../../../core/perms';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { downloadCsv } from '../../../shared/utils/csv-export';
import { fetchAllPages } from '../../../shared/utils/fetch-all-pages';
import { EXPENSE_CATEGORIES, ExpenseDto } from '../expense.model';
import { ExpenseService } from '../expense.service';

@Component({
  selector: 'app-expense-list',
  standalone: true,
  imports: [DatePipe, FormsModule, ReactiveFormsModule, PaginationComponent],
  templateUrl: './expense-list.component.html',
  styleUrl: './expense-list.component.scss'
})
export class ExpenseListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly expenseService = inject(ExpenseService);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

  protected readonly categories = EXPENSE_CATEGORIES;

  protected readonly items = signal<ExpenseDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly formVisible = signal(false);
  protected readonly editingId = signal<number | null>(null);

  protected filterCategory = '';
  protected filterFromDate = '';
  protected filterToDate = '';

  protected readonly form = this.fb.nonNullable.group({
    category: [0, Validators.required],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    description: [''],
    expenseDate: [this.todayIso(), Validators.required]
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

  protected applyFilters(): void {
    this.pageNumber.set(1);
    this.load();
  }

  protected clearFilters(): void {
    this.filterCategory = '';
    this.filterFromDate = '';
    this.filterToDate = '';
    this.pageNumber.set(1);
    this.load();
  }

  protected startCreate(): void {
    this.editingId.set(null);
    this.form.reset({ category: 0, amount: 0, description: '', expenseDate: this.todayIso() });
    this.formVisible.set(true);
  }

  protected startEdit(expense: ExpenseDto): void {
    this.editingId.set(expense.id);
    this.form.reset({
      category: this.categories.find((c) => c.label === expense.category)?.value ?? 0,
      amount: expense.amount,
      description: expense.description ?? '',
      expenseDate: expense.expenseDate.substring(0, 10)
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
      // The native <select>'s value comes back as a string even though the control was seeded with a
      // number - coerce it back before sending, or the backend rejects "3" as an ExpenseCategory.
      category: Number(raw.category),
      amount: raw.amount,
      description: raw.description || null,
      expenseDate: `${raw.expenseDate}T00:00:00.000Z`
    };

    this.saving.set(true);
    const id = this.editingId();
    const result$ = id === null ? this.expenseService.create(request) : this.expenseService.update(id, request);

    result$.subscribe({
      next: () => {
        this.saving.set(false);
        this.formVisible.set(false);
        this.toast.success('Expense saved.');
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not save expense.');
      }
    });
  }

  protected delete(expense: ExpenseDto): void {
    if (!confirm(`Delete this ${expense.category} expense of ${expense.amount}?`)) {
      return;
    }

    this.expenseService.delete(expense.id).subscribe({
      next: () => {
        this.toast.success('Expense deleted.');
        this.load();
      },
      error: () => this.toast.error('Could not delete expense.')
    });
  }

  protected print(): void {
    window.print();
  }

  protected exportCsv(): void {
    fetchAllPages((pageNumber) =>
      this.expenseService.getPaged({
        pageNumber,
        pageSize: 100,
        category: this.filterCategory === '' ? null : Number(this.filterCategory),
        fromDate: this.filterFromDate || null,
        toDate: this.filterToDate || null
      })
    ).subscribe({
      next: (items) => {
        downloadCsv(
          `expenses-${new Date().toISOString().substring(0, 10)}.csv`,
          ['Date', 'Category', 'Description', 'Amount'],
          items.map((e) => [e.expenseDate, e.category, e.description, e.amount])
        );
      },
      error: () => this.toast.error('Could not export expenses.')
    });
  }

  private todayIso(): string {
    return new Date().toISOString().substring(0, 10);
  }

  private load(): void {
    this.loading.set(true);

    this.expenseService
      .getPaged({
        pageNumber: this.pageNumber(),
        pageSize: this.pageSize(),
        category: this.filterCategory === '' ? null : Number(this.filterCategory),
        fromDate: this.filterFromDate || null,
        toDate: this.filterToDate || null
      })
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.toast.error('Could not load expenses.');
        }
      });
  }
}
