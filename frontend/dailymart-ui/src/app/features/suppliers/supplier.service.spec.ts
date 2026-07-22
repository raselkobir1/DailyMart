import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { SupplierDto } from './supplier.model';
import { SupplierService } from './supplier.service';

describe('SupplierService', () => {
  let service: SupplierService;
  let httpMock: HttpTestingController;

  const fakeSupplier: SupplierDto = {
    id: 1,
    name: 'Acme Distributors',
    contactPerson: 'John Doe',
    phone: '0123456789',
    email: null,
    address: null,
    openingBalance: 1000,
    currentDue: 1000
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(SupplierService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getPaged() issues a GET to /suppliers', () => {
    service.getPaged({ pageNumber: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/suppliers');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [fakeSupplier], totalCount: 1, pageNumber: 1, pageSize: 20, totalPages: 1 });
  });

  it('create() posts the request body to /suppliers', () => {
    service
      .create({
        name: 'Acme Distributors',
        contactPerson: 'John Doe',
        phone: '0123456789',
        email: null,
        address: null,
        openingBalance: 1000
      })
      .subscribe();

    const req = httpMock.expectOne('/suppliers');
    expect(req.request.method).toBe('POST');
    req.flush(fakeSupplier);
  });

  it('getLedger() issues a GET to /suppliers/{id}/ledger', () => {
    service.getLedger(1, { pageNumber: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/suppliers/1/ledger');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 0 });
  });
});
