import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PurchaseReturnDto } from './purchase-return.model';
import { PurchaseReturnService } from './purchase-return.service';

describe('PurchaseReturnService', () => {
  let service: PurchaseReturnService;
  let httpMock: HttpTestingController;

  const fakeReturn: PurchaseReturnDto = {
    id: 1,
    returnNumber: 'PRET-000001',
    purchaseId: 1,
    purchaseNumber: 'PUR-000001',
    returnDate: '2026-07-22T00:00:00+00:00',
    totalAmount: 50,
    notes: null,
    items: []
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(PurchaseReturnService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getPaged() issues a GET to /purchases/{purchaseId}/returns', () => {
    service.getPaged(1, { pageNumber: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/purchases/1/returns');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [fakeReturn], totalCount: 1, pageNumber: 1, pageSize: 20, totalPages: 1 });
  });

  it('getById() issues a GET to /purchases/{purchaseId}/returns/{returnId}', () => {
    service.getById(1, 1).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/purchases/1/returns/1');
    expect(req.request.method).toBe('GET');
    req.flush(fakeReturn);
  });

  it('create() posts the request body to /purchases/{purchaseId}/returns', () => {
    service
      .create(1, { returnDate: '2026-07-22T00:00:00.000Z', notes: null, items: [{ purchaseItemId: 5, quantity: 1 }] })
      .subscribe();

    const req = httpMock.expectOne('/purchases/1/returns');
    expect(req.request.method).toBe('POST');
    req.flush(fakeReturn);
  });
});
