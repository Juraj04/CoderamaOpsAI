import { axiosInstance } from './axiosInstance';
import type { Product, CreateProductRequest } from '../types/product.types';

export const productsApi = {
  getAll: async (): Promise<Product[]> => {
    const response = await axiosInstance.get<Product[]>('/api/products');
    return response.data;
  },

  getById: async (id: number): Promise<Product> => {
    const response = await axiosInstance.get<Product>(`/api/products/${id}`);
    return response.data;
  },

  create: async (product: CreateProductRequest): Promise<Product> => {
    const response = await axiosInstance.post<Product>('/api/products', product);
    return response.data;
  },
};
