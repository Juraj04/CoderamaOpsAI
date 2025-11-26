import axios from 'axios';
import { API_URL, ROUTES } from '../utils/constants';
import { storage } from '../utils/storage';

// Create axios instance
export const axiosInstance = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor - add auth token
axiosInstance.interceptors.request.use(
  (config) => {
    const token = storage.getToken();

    // Check if token is expired before making request
    if (token && storage.isTokenExpired()) {
      storage.clearAuth();
      window.location.href = ROUTES.LOGIN;
      return Promise.reject(new Error('Token expired'));
    }

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor - handle 401 errors
axiosInstance.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Token is invalid or expired
      storage.clearAuth();

      // Only redirect if not already on login page
      if (window.location.pathname !== ROUTES.LOGIN) {
        window.location.href = ROUTES.LOGIN;
        // Optionally show toast notification
        // toast.error('Session expired. Please login again.');
      }
    }

    return Promise.reject(error);
  }
);
