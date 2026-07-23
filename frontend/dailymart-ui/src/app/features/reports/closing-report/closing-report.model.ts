export interface ClosingReport {
  periodType: string;
  fromDate: string;
  toDate: string;
  revenue: number;
  salesCount: number;
  cogs: number;
  grossProfit: number;
  totalPurchases: number;
  purchasesCount: number;
  totalExpenses: number;
  netProfit: number;
  cashIn: number;
  cashOut: number;
  netCashFlow: number;
}
