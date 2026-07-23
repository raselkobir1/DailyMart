export interface SupplierDto {
  id: number;
  name: string;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  address: string | null;
  openingBalance: number;
  currentDue: number;
}

/** The update shape - no openingBalance (write-once, only creation can set it). */
export interface SupplierRequest {
  name: string;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  address: string | null;
}

export interface CreateSupplierRequest extends SupplierRequest {
  openingBalance: number;
}

export interface SupplierLedgerEntryDto {
  id: number;
  supplierId: number;
  entryType: string;
  description: string | null;
  amount: number;
  balanceAfter: number;
  transactionDate: string;
}

export interface PaySupplierRequest {
  amount: number;
  notes: string | null;
}
