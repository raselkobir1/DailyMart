/** Ordinals match the backend's PaymentType enum (Cash=0, Credit=1, Partial=2) - reuses the same enum
 * Purchases already established (see purchase.model.ts). */
export const PAYMENT_TYPES = [
  { value: 0, label: 'Cash' },
  { value: 1, label: 'Credit' },
  { value: 2, label: 'Partial' }
] as const;

export interface SaleItemDto {
  id: number;
  productId: number;
  productName: string;
  productCode: string;
  quantity: number;
  unitPrice: number;
  unitCost: number;
  discountAmount: number;
  lineTotal: number;
}

export interface SaleDto {
  id: number;
  saleNumber: string;
  customerId: number | null;
  customerName: string | null;
  saleDate: string;
  paymentType: string;
  subtotalAmount: number;
  discountAmount: number;
  vatAmount: number;
  totalAmount: number;
  paidAmount: number;
  dueAmount: number;
  totalCost: number;
  profitAmount: number;
  notes: string | null;
  items: SaleItemDto[];
}

export interface SaleItemRequest {
  productId: number;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
}

/** Create-only - matches the backend's SaleRequestDto. customerId is null for a walk-in Cash sale; the
 * server rejects Credit/Partial requests with no customer. */
export interface SaleRequest {
  customerId: number | null;
  saleDate: string;
  paymentType: number;
  discountAmount: number;
  vatAmount: number;
  paidAmount: number;
  notes: string | null;
  items: SaleItemRequest[];
}
