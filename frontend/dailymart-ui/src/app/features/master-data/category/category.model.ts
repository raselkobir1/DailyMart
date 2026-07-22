export interface CategoryDto {
  id: number;
  name: string;
  description: string | null;
}

export interface CategoryRequest {
  name: string;
  description: string | null;
}
