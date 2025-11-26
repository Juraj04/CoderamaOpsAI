export const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export const STORAGE_KEYS = {
  AUTH_TOKEN: 'auth_token',
  AUTH_USER: 'auth_user',
  TOKEN_EXPIRY: 'token_expiry',
} as const;

export const ROUTES = {
  LOGIN: '/login',
  PRODUCTS: '/products',
  ORDERS: '/orders',
  PRODUCT_DETAIL: '/products/:id',
} as const;
