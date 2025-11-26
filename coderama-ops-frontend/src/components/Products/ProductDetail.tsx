import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { productsApi } from '../../api/products.api';
import type { Product } from '../../types/product.types';
import { ROUTES } from '../../utils/constants';
import toast from 'react-hot-toast';

export const ProductDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [product, setProduct] = useState<Product | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const fetchProduct = async () => {
      if (!id) return;

      try {
        setIsLoading(true);
        const data = await productsApi.getById(parseInt(id));
        setProduct(data);
      } catch (error: any) {
        if (error.response?.status === 404) {
          toast.error('Product not found');
        } else {
          toast.error('Failed to load product');
        }
        navigate(ROUTES.PRODUCTS);
      } finally {
        setIsLoading(false);
      }
    };

    fetchProduct();
  }, [id, navigate]);

  if (isLoading) {
    return <div className="text-center py-8">Loading product...</div>;
  }

  if (!product) {
    return null;
  }

  return (
    <div className="max-w-2xl mx-auto">
      <button
        onClick={() => navigate(ROUTES.PRODUCTS)}
        className="mb-6 text-blue-600 hover:text-blue-800 transition"
      >
        ← Back to Products
      </button>

      <div className="bg-white rounded-lg shadow-lg p-8">
        <h1 className="text-3xl font-bold mb-4">{product.name}</h1>

        <div className="mb-6">
          <p className="text-gray-600">{product.description || 'No description available'}</p>
        </div>

        <div className="grid grid-cols-2 gap-4 mb-6">
          <div>
            <span className="text-gray-500 text-sm">Price</span>
            <p className="text-3xl font-bold text-blue-600">${product.price.toFixed(2)}</p>
          </div>
          <div>
            <span className="text-gray-500 text-sm">Stock</span>
            <p className="text-2xl font-semibold">{product.stock} units</p>
          </div>
        </div>

        <div className="text-sm text-gray-500">
          <p>Created: {new Date(product.createdAt).toLocaleString()}</p>
        </div>
      </div>
    </div>
  );
};
