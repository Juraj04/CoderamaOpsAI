import { axiosInstance } from './axiosInstance';
import type { LoginRequest, LoginResponse } from '../types/auth.types';

export const authApi = {
  login: async (credentials: LoginRequest): Promise<LoginResponse> => {
    const response = await axiosInstance.post<LoginResponse>(
      '/api/auth/login',
      credentials
    );
    return response.data;
  },
};
