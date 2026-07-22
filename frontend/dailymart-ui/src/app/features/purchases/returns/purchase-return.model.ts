export interface PurchaseReturnItemDto {
  id: number;
  purchaseItemId: number;
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface PurchaseReturnDto {
  id: number;
  returnNumber: string;
  purchaseId: number;
  purchaseNumber: string;
  returnDate: string;
  totalAmount: number;
  notes: string | null;
  items: PurchaseReturnItemDto[];
}

export interface PurchaseReturnItemRequest {
  purchaseItemId: number;
  quantity: number;
}

/** PurchaseId isn't part of the body - it comes from the route, matching the backend's
 * PurchaseReturnRequestDto. */
export interface PurchaseReturnRequest {
  returnDate: string;
  notes: string | null;
  items: PurchaseReturnItemRequest[];
}
