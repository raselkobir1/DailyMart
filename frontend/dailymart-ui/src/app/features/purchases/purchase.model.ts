/** Ordinals match the backend's PaymentType enum (Cash=0, Credit=1, Partial=2) - the API has no
 * JsonStringEnumConverter registered, so PurchaseRequestDto.paymentType serializes as a number. */
export const PAYMENT_TYPES = [
  { value: 0, label: 'Cash' },
  { value: 1, label: 'Credit' },
  { value: 2, label: 'Partial' }
] as const;

/** Maps PurchaseDto.paymentType (a string, since the response DTO already converts the enum) back to
 * its numeric ordinal for pre-filling the edit form's paymentType select. */
export const PAYMENT_TYPE_VALUES: Record<string, number> = { Cash: 0, Credit: 1, Partial: 2 };

export interface PurchaseItemDto {
  id: number;
  productId: number;
  productName: string;
  productCode: string;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
  lineTotal: number;
}

export interface PurchaseDto {
  id: number;
  purchaseNumber: string;
  supplierId: number;
  supplierName: string;
  purchaseDate: string;
  paymentType: string;
  subtotalAmount: number;
  discountAmount: number;
  vatAmount: number;
  totalAmount: number;
  paidAmount: number;
  dueAmount: number;
  notes: string | null;
  items: PurchaseItemDto[];
}

export interface PurchaseItemRequest {
  productId: number;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
}

/** One shape for both create and update - matches the backend's PurchaseRequestDto. paidAmount is only
 * meaningful when paymentType is Partial (2); the server derives it otherwise. */
export interface PurchaseRequest {
  supplierId: number;
  purchaseDate: string;
  paymentType: number;
  discountAmount: number;
  vatAmount: number;
  paidAmount: number;
  notes: string | null;
  items: PurchaseItemRequest[];
}
