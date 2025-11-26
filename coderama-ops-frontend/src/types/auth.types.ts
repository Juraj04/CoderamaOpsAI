export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string; // ISO 8601 date string
  userId: number;
  email: string;
  name: string;
}

export interface User {
  email: string;
  name: string;
  userId: number;
}
