import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ProductDto } from './product.model';
import { ProductService } from './product.service';

describe('ProductService', () => {
  let service: ProductService;
  let httpMock: HttpTestingController;

  const fakeProduct: ProductDto = {
    id: 1,
    code: 'P-001',
    barcode: '2001234567895',
    name: 'Rice 1kg',
    categoryId: 1,
    categoryName: 'Grocery',
    brandId: null,
    brandName: null,
    unitId: 1,
    unitName: 'Kilogram',
    unitSymbol: 'kg',
    purchasePrice: 50,
    sellingPrice: 60,
    wholesalePrice: null,
    discountPercentage: 0,
    taxPercentage: 0,
    currentStock: 10,
    minimumStock: 5,
    allowPriceBelowCost: false,
    imageUrl: null
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(ProductService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getPaged() issues a GET to /products with query params', () => {
    service.getPaged({ pageNumber: 1, pageSize: 20, searchTerm: 'rice' }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/products' && r.params.get('searchTerm') === 'rice');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [fakeProduct], totalCount: 1, pageNumber: 1, pageSize: 20, totalPages: 1 });
  });

  it('create() posts the request body to /products', () => {
    service
      .create({
        code: 'P-001',
        barcode: null,
        name: 'Rice 1kg',
        categoryId: 1,
        brandId: null,
        unitId: 1,
        purchasePrice: 50,
        sellingPrice: 60,
        wholesalePrice: null,
        discountPercentage: 0,
        taxPercentage: 0,
        minimumStock: 5,
        allowPriceBelowCost: false,
        currentStock: 10
      })
      .subscribe();

    const req = httpMock.expectOne('/products');
    expect(req.request.method).toBe('POST');
    req.flush(fakeProduct);
  });

  it('uploadImage() posts a FormData body to /products/{id}/image', () => {
    const file = new File(['bytes'], 'photo.png', { type: 'image/png' });

    service.uploadImage(1, file).subscribe();

    const req = httpMock.expectOne('/products/1/image');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeInstanceOf(FormData);
    req.flush(fakeProduct);
  });

  it('exportCsv() requests /products/export as a blob', () => {
    service.exportCsv().subscribe();

    const req = httpMock.expectOne('/products/export');
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(new Blob(['csv content']));
  });

  it('getLowStock() issues a GET to /products/low-stock', () => {
    service.getLowStock({ pageNumber: 1, pageSize: 20 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/products/low-stock');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [fakeProduct], totalCount: 1, pageNumber: 1, pageSize: 20, totalPages: 1 });
  });
});
