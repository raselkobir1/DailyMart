import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../shared/models/paged-result.model';
import { DamagedStockRequest, InventoryAdjustmentDto, InventoryTransactionDto, StockAdjustmentRequest } from './inventory.model';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly http = inject(HttpClient);

  recordAdjustment(request: StockAdjustmentRequest): Observable<InventoryAdjustmentDto> {
    return this.http.post<InventoryAdjustmentDto>('/inventory/adjustments', request);
  }

  recordDamaged(request: DamagedStockRequest): Observable<InventoryAdjustmentDto> {
    return this.http.post<InventoryAdjustmentDto>('/inventory/damaged', request);
  }

  getTransactionHistory(
    request: PagedRequest,
    productId?: number | null
  ): Observable<PagedResult<InventoryTransactionDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);
    if (productId) params = params.set('productId', productId);

    return this.http.get<PagedResult<InventoryTransactionDto>>('/inventory/transactions', { params });
  }
}
