import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CustomerDto } from './customer.model';
import { CustomerService } from './customer.service';

describe('CustomerService', () => {
  let service: CustomerService;
  let httpMock: HttpTestingController;

  const fakeCustomer: CustomerDto = {
    id: 1,
    name: 'Karim Ahmed',
    phone: '01711111111',
    email: null,
    address: null,
    currentDue: 0
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(CustomerService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getPaged() issues a GET to /customers', () => {
    service.getPaged({ pageNumber: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/customers');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [fakeCustomer], totalCount: 1, pageNumber: 1, pageSize: 20, totalPages: 1 });
  });

  it('create() posts the request body to /customers', () => {
    service.create({ name: 'Karim Ahmed', phone: '01711111111', email: null, address: null }).subscribe();

    const req = httpMock.expectOne('/customers');
    expect(req.request.method).toBe('POST');
    req.flush(fakeCustomer);
  });

  it('delete() issues a DELETE to /customers/{id}', () => {
    service.delete(1).subscribe();

    const req = httpMock.expectOne('/customers/1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
