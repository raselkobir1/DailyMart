export interface SaleReturnItemDto {
  id: number;
  saleItemId: number;
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface SaleReturnDto {
  id: number;
  returnNumber: string;
  saleId: number;
  saleNumber: string;
  returnDate: string;
  totalAmount: number;
  notes: string | null;
  items: SaleReturnItemDto[];
}

export interface SaleReturnItemRequest {
  saleItemId: number;
  quantity: number;
}

/** SaleId isn't part of the body - it comes from the route, matching the backend's SaleReturnRequestDto. */
export interface SaleReturnRequest {
  returnDate: string;
  notes: string | null;
  items: SaleReturnItemRequest[];
}
