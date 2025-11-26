import React, { useState, useEffect } from 'react';
import { ordersApi } from '../../api/orders.api';
import { productsApi } from '../../api/products.api';
import { OrderStatus } from '../../types/order.types';
import type { CreateOrderRequest } from '../../types/order.types';
import type { Product } from '../../types/product.types';
import { storage } from '../../utils/storage';
import toast from 'react-hot-toast';

interface OrderFormProps {
  onSuccess: () => void;
  onCancel: () => void;
}

export const OrderForm: React.FC<OrderFormProps> = ({ onSuccess, onCancel }) => {
  const [products, setProducts] = useState<Product[]>([]);
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    const fetchProducts = async () => {
      try {
        const data = await productsApi.getAll();
        setProducts(data);
      } catch (error) {
        toast.error('Failed to load products');
      }
    };
    fetchProducts();
  }, []);

  const handleProductChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const productId = parseInt(e.target.value);
    const product = products.find(p => p.id === productId) || null;
    setSelectedProduct(product);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!selectedProduct) {
      toast.error('Please select a product');
      return;
    }

    if (quantity < 1) {
      toast.error('Quantity must be at least 1');
      return;
    }

    if (quantity > selectedProduct.stock) {
      toast.error(`Only ${selectedProduct.stock} units available`);
      return;
    }

    // Get current user
    const user = storage.getUser();
    if (!user) {
      toast.error('User not found');
      return;
    }

    const orderRequest: CreateOrderRequest = {
      userId: user.userId,
      productId: selectedProduct.id,
      quantity: quantity,
      price: selectedProduct.price,
      status: OrderStatus.Pending,
    };

    setIsSubmitting(true);

    try {
      await ordersApi.create(orderRequest);

      // Show success notification (requirement from section 1)
      toast.success('Order created successfully!');

      onSuccess();
    } catch (error: any) {
      const message = error.response?.data?.message || 'Failed to create order';
      toast.error(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  const calculateTotal = () => {
    return selectedProduct ? selectedProduct.price * quantity : 0;
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg p-6 max-w-md w-full">
        <h2 className="text-2xl font-bold mb-4">Create Order</h2>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Product *
            </label>
            <select
              required
              onChange={handleProductChange}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">Select a product</option>
              {products.map(product => (
                <option key={product.id} value={product.id}>
                  {product.name} - ${product.price.toFixed(2)} (Stock: {product.stock})
                </option>
              ))}
            </select>
          </div>

          {selectedProduct && (
            <>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Quantity * (max: {selectedProduct.stock})
                </label>
                <input
                  type="number"
                  required
                  min="1"
                  max={selectedProduct.stock}
                  value={quantity}
                  onChange={(e) => setQuantity(parseInt(e.target.value) || 1)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div className="bg-gray-50 p-4 rounded-md">
                <div className="flex justify-between mb-2">
                  <span className="text-gray-600">Unit Price:</span>
                  <span className="font-semibold">${selectedProduct.price.toFixed(2)}</span>
                </div>
                <div className="flex justify-between mb-2">
                  <span className="text-gray-600">Quantity:</span>
                  <span className="font-semibold">{quantity}</span>
                </div>
                <div className="flex justify-between pt-2 border-t border-gray-300">
                  <span className="font-bold text-lg">Total:</span>
                  <span className="font-bold text-lg text-blue-600">
                    ${calculateTotal().toFixed(2)}
                  </span>
                </div>
              </div>
            </>
          )}

          <div className="flex space-x-3 pt-4">
            <button
              type="submit"
              disabled={isSubmitting || !selectedProduct}
              className="flex-1 py-2 px-4 bg-blue-600 hover:bg-blue-700 text-white rounded-md transition disabled:bg-gray-400"
            >
              {isSubmitting ? 'Creating...' : 'Create Order'}
            </button>
            <button
              type="button"
              onClick={onCancel}
              className="flex-1 py-2 px-4 bg-gray-300 hover:bg-gray-400 text-gray-800 rounded-md transition"
            >
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
