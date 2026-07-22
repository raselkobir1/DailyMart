import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { InventoryAdjustmentDto, InventoryTransactionDto } from './inventory.model';
import { InventoryService } from './inventory.service';

describe('InventoryService', () => {
  let service: InventoryService;
  let httpMock: HttpTestingController;

  const fakeAdjustment: InventoryAdjustmentDto = {
    id: 1,
    productId: 1,
    productName: 'Rice 5kg',
    productCode: 'P001',
    adjustmentType: 'Adjustment',
    quantityChange: -3,
    reason: 'Recount',
    adjustmentDate: '2026-07-22T00:00:00+00:00'
  };

  const fakeTransaction: InventoryTransactionDto = {
    id: 1,
    productId: 1,
    productName: 'Rice 5kg',
    productCode: 'P001',
    transactionType: 'Adjustment',
    quantityChange: -3,
    balanceAfter: 7,
    referenceType: 'InventoryAdjustment',
    referenceId: 1,
    notes: 'Recount',
    transactionDate: '2026-07-22T00:00:00+00:00'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(InventoryService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('recordAdjustment() posts the request body to /inventory/adjustments', () => {
    service.recordAdjustment({ productId: 1, newStockCount: 7, reason: 'Recount' }).subscribe();

    const req = httpMock.expectOne('/inventory/adjustments');
    expect(req.request.method).toBe('POST');
    req.flush(fakeAdjustment);
  });

  it('recordDamaged() posts the request body to /inventory/damaged', () => {
    service.recordDamaged({ productId: 1, quantity: 3, reason: 'Broken in transit' }).subscribe();

    const req = httpMock.expectOne('/inventory/damaged');
    expect(req.request.method).toBe('POST');
    req.flush(fakeAdjustment);
  });

  it('getTransactionHistory() issues a GET to /inventory/transactions with an optional productId', () => {
    service.getTransactionHistory({ pageNumber: 1, pageSize: 20 }, 1).subscribe();

    const req = httpMock.expectOne(
      (r) => r.url === '/inventory/transactions' && r.params.get('productId') === '1'
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [fakeTransaction], totalCount: 1, pageNumber: 1, pageSize: 20, totalPages: 1 });
  });
});
