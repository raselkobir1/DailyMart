import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Perms } from '../../../../core/perms';
import { Toast } from '../../../../core/toast';
import { PaginationComponent } from '../../../../shared/pagination/pagination.component';
import { UnitDto } from '../unit.model';
import { UnitService } from '../unit.service';

@Component({
  selector: 'app-unit-list',
  standalone: true,
  imports: [ReactiveFormsModule, PaginationComponent],
  templateUrl: './unit-list.component.html',
  styleUrl: './unit-list.component.scss'
})
export class UnitListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly unitService = inject(UnitService);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

  protected readonly items = signal<UnitDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly formVisible = signal(false);
  protected readonly editingId = signal<number | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(50)]],
    symbol: ['', [Validators.required, Validators.maxLength(10)]]
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
    this.form.reset({ name: '', symbol: '' });
    this.formVisible.set(true);
  }

  protected startEdit(unit: UnitDto): void {
    this.editingId.set(unit.id);
    this.form.reset({ name: unit.name, symbol: unit.symbol });
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

    const request = this.form.getRawValue();

    this.saving.set(true);
    const id = this.editingId();
    const result$ = id === null ? this.unitService.create(request) : this.unitService.update(id, request);

    result$.subscribe({
      next: () => {
        this.saving.set(false);
        this.formVisible.set(false);
        this.toast.success('Unit saved.');
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not save unit.');
      }
    });
  }

  protected delete(unit: UnitDto): void {
    if (!confirm(`Delete unit "${unit.name}"?`)) {
      return;
    }

    this.unitService.delete(unit.id).subscribe({
      next: () => {
        this.toast.success('Unit deleted.');
        this.load();
      },
      error: () => this.toast.error('Could not delete unit.')
    });
  }

  private load(): void {
    this.loading.set(true);

    this.unitService.getPaged({ pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load units.');
      }
    });
  }
}
