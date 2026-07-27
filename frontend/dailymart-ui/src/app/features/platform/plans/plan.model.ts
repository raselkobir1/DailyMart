export interface PlanDto {
  id: number;
  name: string;
  description: string | null;
  price: number;
  billingCycle: string;
  isFree: boolean;
  isActive: boolean;
  sortOrder: number;
  createdAt: string;
}

/** billingCycle is sent as 0 (Monthly) or 1 (Yearly) - matches the backend's BillingCycle enum, same
 * numeric-by-default convention as PaymentType elsewhere (see sale.model.ts). */
export interface PlanRequest {
  name: string;
  description: string | null;
  price: number;
  billingCycle: number;
  isFree: boolean;
  sortOrder: number;
}

export const BILLING_CYCLES = [
  { value: 0, label: 'Monthly' },
  { value: 1, label: 'Yearly' }
];
