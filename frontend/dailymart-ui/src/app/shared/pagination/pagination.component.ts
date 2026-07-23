import { Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  template: `
    <div class="pagination">
      <span>{{ rangeText() }}</span>
      <select class="input" [value]="pageSize()" (change)="onSizeChange($event)">
        @for (size of sizes(); track size) {
          <option [value]="size">{{ size }} / page</option>
        }
      </select>
      <div class="pages">
        <button type="button" class="btn btn-sm" [disabled]="pageNumber() <= 1" (click)="pageChange.emit(pageNumber() - 1)">
          ‹ Prev
        </button>
        <button type="button" class="btn btn-sm" [disabled]="pageNumber() >= totalPages()" (click)="pageChange.emit(pageNumber() + 1)">
          Next ›
        </button>
      </div>
    </div>
  `
})
export class PaginationComponent {
  readonly total = input.required<number>();
  readonly pageNumber = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly sizes = input<number[]>([10, 20, 50]);

  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));

  protected readonly rangeText = computed(() => {
    if (this.total() === 0) {
      return 'No results';
    }
    const start = (this.pageNumber() - 1) * this.pageSize() + 1;
    const end = Math.min(this.total(), this.pageNumber() * this.pageSize());
    return `${start}–${end} of ${this.total()}`;
  });

  protected onSizeChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.pageSizeChange.emit(value);
  }
}
