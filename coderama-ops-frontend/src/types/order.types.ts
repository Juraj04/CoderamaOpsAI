export const OrderStatus = {
  Pending: 0,
  Processing: 1,
  Completed: 2,
  Expired: 3
} as const;

export interface Order {
  id: number;
  userId: number;
  userName: string;
  productId: number;
  productName: string;
  quantity: number;
  price: number;
  total: number;
  status: string; // "Pending" | "Processing" | "Completed" | "Expired"
  createdAt: string;
  updatedAt: string;
}

export interface CreateOrderRequest {
  userId: number;
  productId: number;
  quantity: number;
  price: number;
  status: number;
}
