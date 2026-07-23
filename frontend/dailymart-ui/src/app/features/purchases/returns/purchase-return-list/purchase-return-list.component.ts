import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Perms } from '../../../../core/perms';
import { Toast } from '../../../../core/toast';
import { PaginationComponent } from '../../../../shared/pagination/pagination.component';
import { PurchaseDto } from '../../purchase.model';
import { PurchaseService } from '../../purchase.service';
import { PurchaseReturnDto } from '../purchase-return.model';
import { PurchaseReturnService } from '../purchase-return.service';

/** Read-only - no edit/delete, since PurchaseReturn is create+read only (IPurchaseReturnService). */
@Component({
  selector: 'app-purchase-return-list',
  standalone: true,
  imports: [DatePipe, PaginationComponent],
  templateUrl: './purchase-return-list.component.html',
  styleUrl: './purchase-return-list.component.scss'
})
export class PurchaseReturnListComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly purchaseService = inject(PurchaseService);
  private readonly purchaseReturnService = inject(PurchaseReturnService);
  private readonly toast = inject(Toast);
  protected readonly perms = inject(Perms);

  private readonly purchaseId = Number(this.route.snapshot.paramMap.get('id'));

  protected readonly purchase = signal<PurchaseDto | null>(null);
  protected readonly items = signal<PurchaseReturnDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageNumber = signal(1);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    this.purchaseService.getById(this.purchaseId).subscribe({
      next: (purchase) => this.purchase.set(purchase),
      error: () => this.toast.error('Could not load purchase.')
    });

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

  protected back(): void {
    this.router.navigateByUrl('/purchases');
  }

  protected createReturn(): void {
    this.router.navigateByUrl(`/purchases/${this.purchaseId}/returns/new`);
  }

  private load(): void {
    this.loading.set(true);

    this.purchaseReturnService
      .getPaged(this.purchaseId, { pageNumber: this.pageNumber(), pageSize: this.pageSize() })
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.toast.error('Could not load returns.');
        }
      });
  }
}
