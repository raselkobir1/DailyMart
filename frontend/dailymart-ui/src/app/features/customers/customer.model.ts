export interface CustomerDto {
  id: number;
  name: string;
  phone: string | null;
  email: string | null;
  address: string | null;
}

/** Used for both create and update - the shape is identical either way (Module 6 Step 6). */
export interface CustomerRequest {
  name: string;
  phone: string | null;
  email: string | null;
  address: string | null;
}
