import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Toast } from '../../../core/toast';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { SaleDto } from '../sale.model';
import { SaleService } from '../sale.service';

/** Read-only history - no edit/delete, since Sale is create+read only (ISaleService). */
@Component({
  selector: 'app-sale-list',
  standalone: true,
  imports: [DatePipe, PaginationComponent],
  templateUrl: './sale-list.component.html',
  styleUrl: './sale-list.component.scss'
})
export class SaleListComponent implements OnInit {
  private readonly saleService = inject(SaleService);
  private readonly router = inject(Router);
  private readonly toast = inject(Toast);

  protected readonly items = signal<SaleDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(false);

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

  protected newSale(): void {
    this.router.navigateByUrl('/pos');
  }

  protected view(sale: SaleDto): void {
    this.router.navigateByUrl(`/sales/${sale.id}`);
  }

  protected viewReturns(sale: SaleDto): void {
    this.router.navigateByUrl(`/sales/${sale.id}/returns`);
  }

  private load(): void {
    this.loading.set(true);

    this.saleService.getPaged({ pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load sales.');
      }
    });
  }
}
