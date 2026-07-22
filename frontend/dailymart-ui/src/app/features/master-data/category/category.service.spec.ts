import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CategoryDto } from './category.model';
import { CategoryService } from './category.service';

describe('CategoryService', () => {
  let service: CategoryService;
  let httpMock: HttpTestingController;

  const fakeCategory: CategoryDto = { id: 1, name: 'Grocery', description: 'Food items' };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(CategoryService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getPaged() issues a GET to /categories with query params', () => {
    service.getPaged({ pageNumber: 2, pageSize: 10, searchTerm: 'gro' }).subscribe();

    const req = httpMock.expectOne(
      (r) => r.url === '/categories' && r.params.get('pageNumber') === '2' && r.params.get('searchTerm') === 'gro'
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [fakeCategory], totalCount: 1, pageNumber: 2, pageSize: 10, totalPages: 1 });
  });

  it('create() issues a POST with the request body', () => {
    service.create({ name: 'Grocery', description: 'Food items' }).subscribe();

    const req = httpMock.expectOne('/categories');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ name: 'Grocery', description: 'Food items' });
    req.flush(fakeCategory);
  });

  it('update() issues a PUT to /categories/{id}', () => {
    service.update(1, { name: 'Groceries', description: null }).subscribe();

    const req = httpMock.expectOne('/categories/1');
    expect(req.request.method).toBe('PUT');
    req.flush({ ...fakeCategory, name: 'Groceries' });
  });

  it('delete() issues a DELETE to /categories/{id}', () => {
    service.delete(1).subscribe();

    const req = httpMock.expectOne('/categories/1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
