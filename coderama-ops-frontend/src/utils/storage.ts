import { STORAGE_KEYS } from './constants';
import type { User } from '../types/auth.types';

export const storage = {
  setToken: (token: string) => {
    localStorage.setItem(STORAGE_KEYS.AUTH_TOKEN, token);
  },

  getToken: (): string | null => {
    return localStorage.getItem(STORAGE_KEYS.AUTH_TOKEN);
  },

  setUser: (user: User) => {
    localStorage.setItem(STORAGE_KEYS.AUTH_USER, JSON.stringify(user));
  },

  getUser: (): User | null => {
    const userStr = localStorage.getItem(STORAGE_KEYS.AUTH_USER);
    return userStr ? JSON.parse(userStr) : null;
  },

  setTokenExpiry: (expiresAt: string) => {
    localStorage.setItem(STORAGE_KEYS.TOKEN_EXPIRY, expiresAt);
  },

  getTokenExpiry: (): string | null => {
    return localStorage.getItem(STORAGE_KEYS.TOKEN_EXPIRY);
  },

  isTokenExpired: (): boolean => {
    const expiry = storage.getTokenExpiry();
    if (!expiry) return true;
    return new Date(expiry) <= new Date();
  },

  clearAuth: () => {
    localStorage.removeItem(STORAGE_KEYS.AUTH_TOKEN);
    localStorage.removeItem(STORAGE_KEYS.AUTH_USER);
    localStorage.removeItem(STORAGE_KEYS.TOKEN_EXPIRY);
  },
};
