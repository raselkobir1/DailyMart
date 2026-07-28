export interface TenantSubscriptionDto {
  tenantId: number;
  tenantName: string;
  planId: number;
  planName: string;
  isFree: boolean;
  price: number;
  currentPeriodStart: string;
  currentPeriodEnd: string | null;
  isOverdue: boolean;
}

export interface SubscriptionPaymentDto {
  id: number;
  amount: number;
  periodStart: string;
  periodEnd: string;
  method: string;
  notes: string | null;
  createdAt: string;
  createdBy: string;
}

export interface ChangePlanRequest {
  planId: number;
}

export interface RecordPaymentRequest {
  amount: number;
  paidUntil: string;
  method: string;
  notes: string | null;
}

export interface TenantReminderEmailResult {
  sentTo: string;
  reminderType: 'Overdue' | 'Free';
}
