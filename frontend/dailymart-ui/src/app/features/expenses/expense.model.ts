/** Ordinals match the backend's ExpenseCategory enum (Rent=0, Salary=1, Electricity=2, Internet=3,
 * Miscellaneous=4) - same reasoning as purchase.model.ts's PAYMENT_TYPES. */
export const EXPENSE_CATEGORIES = [
  { value: 0, label: 'Rent' },
  { value: 1, label: 'Salary' },
  { value: 2, label: 'Electricity' },
  { value: 3, label: 'Internet' },
  { value: 4, label: 'Miscellaneous' }
] as const;

export interface ExpenseDto {
  id: number;
  category: string;
  amount: number;
  description: string | null;
  expenseDate: string;
}

/** Used for both create and update - the shape is identical either way. */
export interface ExpenseRequest {
  category: number;
  amount: number;
  description: string | null;
  expenseDate: string;
}
