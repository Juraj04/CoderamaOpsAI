import { axiosInstance } from './axiosInstance';
import type { Order, CreateOrderRequest } from '../types/order.types';

export const ordersApi = {
  getAll: async (): Promise<Order[]> => {
    const response = await axiosInstance.get<Order[]>('/api/orders');
    return response.data;
  },

  getById: async (id: number): Promise<Order> => {
    const response = await axiosInstance.get<Order>(`/api/orders/${id}`);
    return response.data;
  },

  create: async (order: CreateOrderRequest): Promise<Order> => {
    const response = await axiosInstance.post<Order>('/api/orders', order);
    return response.data;
  },

  // Helper to filter orders by userId (CRITICAL - see section 3.2)
  getUserOrders: async (userId: number): Promise<Order[]> => {
    const allOrders = await ordersApi.getAll();
    return allOrders.filter(order => order.userId === userId);
  },
};
