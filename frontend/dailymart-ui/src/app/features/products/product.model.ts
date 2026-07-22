export interface ProductDto {
  id: number;
  code: string;
  barcode: string;
  name: string;
  categoryId: number;
  categoryName: string;
  brandId: number | null;
  brandName: string | null;
  unitId: number;
  unitName: string;
  unitSymbol: string;
  purchasePrice: number;
  sellingPrice: number;
  wholesalePrice: number | null;
  discountPercentage: number;
  taxPercentage: number;
  currentStock: number;
  minimumStock: number;
  allowPriceBelowCost: boolean;
  imageUrl: string | null;
}

/** The update shape - no currentStock (server-side, only creation can set it). */
export interface ProductRequest {
  code: string;
  barcode: string | null;
  name: string;
  categoryId: number;
  brandId: number | null;
  unitId: number;
  purchasePrice: number;
  sellingPrice: number;
  wholesalePrice: number | null;
  discountPercentage: number;
  taxPercentage: number;
  minimumStock: number;
  allowPriceBelowCost: boolean;
}

export interface CreateProductRequest extends ProductRequest {
  currentStock: number;
}
