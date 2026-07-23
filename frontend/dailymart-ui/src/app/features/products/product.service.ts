import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedRequest, PagedResult } from '../../shared/models/paged-result.model';
import { CreateProductRequest, ProductDto, ProductRequest } from './product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);

  getPaged(request: PagedRequest): Observable<PagedResult<ProductDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);
    if (request.searchTerm) params = params.set('searchTerm', request.searchTerm);

    return this.http.get<PagedResult<ProductDto>>('/products', { params });
  }

  getById(id: number): Observable<ProductDto> {
    return this.http.get<ProductDto>(`/products/${id}`);
  }

  /** What the POS barcode-scanner workflow calls - the backend returns 404 (surfaced by errorInterceptor)
   * when nothing matches, same "missing" contract as getById. */
  getByBarcode(barcode: string): Observable<ProductDto> {
    return this.http.get<ProductDto>(`/products/barcode/${barcode}`);
  }

  create(request: CreateProductRequest): Observable<ProductDto> {
    return this.http.post<ProductDto>('/products', request);
  }

  update(id: number, request: ProductRequest): Observable<ProductDto> {
    return this.http.put<ProductDto>(`/products/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/products/${id}`);
  }

  uploadImage(id: number, file: File): Observable<ProductDto> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ProductDto>(`/products/${id}/image`, formData);
  }

  exportCsv(): Observable<Blob> {
    return this.http.get('/products/export', { responseType: 'blob' });
  }

  /** Added for Module 8 (Inventory), but lives here since it only queries Product - see the backend's
   * IProductService.GetLowStockAsync. */
  getLowStock(request: PagedRequest): Observable<PagedResult<ProductDto>> {
    let params = new HttpParams();
    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber);
    if (request.pageSize) params = params.set('pageSize', request.pageSize);

    return this.http.get<PagedResult<ProductDto>>('/products/low-stock', { params });
  }
}
