export interface LowStockProduct {
  productId: number;
  productName: string;
  currentStock: number;
  minimumStock: number;
}

export interface TopSellingProduct {
  productId: number;
  productName: string;
  quantitySold: number;
  revenue: number;
}

export interface DashboardTrendPoint {
  date: string;
  sales: number;
  purchases: number;
  profit: number;
}

export interface DashboardSummary {
  todaySales: number;
  todayPurchases: number;
  todayProfit: number;
  todayExpense: number;
  cashInHand: number;
  totalCustomerDue: number;
  totalSupplierDue: number;
  inventoryValue: number;
  lowStockCount: number;
  lowStockProducts: LowStockProduct[];
  topSellingProducts: TopSellingProduct[];
  salesTrend: DashboardTrendPoint[];
}
