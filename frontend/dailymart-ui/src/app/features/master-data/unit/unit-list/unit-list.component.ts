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
import { UnitDto } from '../unit.model';
import { UnitService } from '../unit.service';

@Component({
  selector: 'app-unit-list',
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
  templateUrl: './unit-list.component.html',
  styleUrl: './unit-list.component.scss'
})
export class UnitListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly unitService = inject(UnitService);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly displayedColumns = ['name', 'symbol', 'actions'];
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

  protected onPageChange(event: PageEvent): void {
    this.pageNumber.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
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
        this.snackBar.open('Unit saved.', 'Dismiss', { duration: 3000 });
        this.load();
      },
      error: (error) => {
        this.saving.set(false);
        this.snackBar.open(error.error?.title ?? 'Could not save unit.', 'Dismiss');
      }
    });
  }

  protected delete(unit: UnitDto): void {
    if (!confirm(`Delete unit "${unit.name}"?`)) {
      return;
    }

    this.unitService.delete(unit.id).subscribe({
      next: () => {
        this.snackBar.open('Unit deleted.', 'Dismiss', { duration: 3000 });
        this.load();
      },
      error: () => this.snackBar.open('Could not delete unit.', 'Dismiss')
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
        this.snackBar.open('Could not load units.', 'Dismiss');
      }
    });
  }
}
