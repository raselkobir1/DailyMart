export interface CustomerDto {
  id: number;
  name: string;
  phone: string | null;
  email: string | null;
  address: string | null;
  currentDue: number;
}

/** Used for both create and update - the shape is identical either way (Module 6 Step 6). Never includes
 * currentDue - that only ever moves via Sale/SaleReturn/Payment, added in Module 9. */
export interface CustomerRequest {
  name: string;
  phone: string | null;
  email: string | null;
  address: string | null;
}

export interface CustomerLedgerEntryDto {
  id: number;
  customerId: number;
  entryType: string;
  description: string | null;
  amount: number;
  balanceAfter: number;
  transactionDate: string;
}
