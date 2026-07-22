export interface BrandDto {
  id: number;
  name: string;
  description: string | null;
}

export interface BrandRequest {
  name: string;
  description: string | null;
}
