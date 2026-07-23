export interface UserDto {
  id: number;
  username: string;
  fullName: string;
  role: string;
  isActive: boolean;
}

export interface CreateUserRequest {
  username: string;
  password: string;
  fullName: string;
  role: string;
}

export interface UpdateUserRequest {
  fullName: string;
  role: string;
  isActive: boolean;
}
