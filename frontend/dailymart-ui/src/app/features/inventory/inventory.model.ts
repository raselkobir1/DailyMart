export interface InventoryTransactionDto {
  id: number;
  productId: number;
  productName: string;
  productCode: string;
  transactionType: string;
  quantityChange: number;
  balanceAfter: number;
  referenceType: string;
  referenceId: number;
  notes: string | null;
  transactionDate: string;
}

export interface InventoryAdjustmentDto {
  id: number;
  productId: number;
  productName: string;
  productCode: string;
  adjustmentType: string;
  quantityChange: number;
  reason: string;
  adjustmentDate: string;
}

/** newStockCount is the actual physical count, not a delta - the server computes the change itself. */
export interface StockAdjustmentRequest {
  productId: number;
  newStockCount: number;
  reason: string;
}

/** quantity is always a positive count of units damaged - the server applies it as a negative change. */
export interface DamagedStockRequest {
  productId: number;
  quantity: number;
  reason: string;
}
