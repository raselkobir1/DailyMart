import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PurchaseDto } from './purchase.model';
import { PurchaseService } from './purchase.service';

describe('PurchaseService', () => {
  let service: PurchaseService;
  let httpMock: HttpTestingController;

  const fakePurchase: PurchaseDto = {
    id: 1,
    purchaseNumber: 'PUR-000001',
    supplierId: 1,
    supplierName: 'Acme Distributors',
    purchaseDate: '2026-07-22T00:00:00+00:00',
    paymentType: 'Cash',
    subtotalAmount: 100,
    discountAmount: 0,
    vatAmount: 0,
    totalAmount: 100,
    paidAmount: 100,
    dueAmount: 0,
    notes: null,
    items: []
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(PurchaseService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getPaged() issues a GET to /purchases', () => {
    service.getPaged({ pageNumber: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/purchases');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [fakePurchase], totalCount: 1, pageNumber: 1, pageSize: 20, totalPages: 1 });
  });

  it('create() posts the request body to /purchases', () => {
    service
      .create({
        supplierId: 1,
        purchaseDate: '2026-07-22T00:00:00.000Z',
        paymentType: 0,
        discountAmount: 0,
        vatAmount: 0,
        paidAmount: 100,
        notes: null,
        items: [{ productId: 1, quantity: 2, unitPrice: 50, discountAmount: 0 }]
      })
      .subscribe();

    const req = httpMock.expectOne('/purchases');
    expect(req.request.method).toBe('POST');
    req.flush(fakePurchase);
  });

  it('update() puts the request body to /purchases/{id}', () => {
    service
      .update(1, {
        supplierId: 1,
        purchaseDate: '2026-07-22T00:00:00.000Z',
        paymentType: 1,
        discountAmount: 0,
        vatAmount: 0,
        paidAmount: 0,
        notes: null,
        items: [{ productId: 1, quantity: 2, unitPrice: 50, discountAmount: 0 }]
      })
      .subscribe();

    const req = httpMock.expectOne('/purchases/1');
    expect(req.request.method).toBe('PUT');
    req.flush(fakePurchase);
  });

  it('delete() issues a DELETE to /purchases/{id}', () => {
    service.delete(1).subscribe();

    const req = httpMock.expectOne('/purchases/1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
