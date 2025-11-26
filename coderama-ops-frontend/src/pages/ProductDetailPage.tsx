import React from 'react';
import { Navigation } from '../components/Layout/Navigation';
import { ProductDetail } from '../components/Products/ProductDetail';

export const ProductDetailPage: React.FC = () => {
  return (
    <>
      <Navigation />
      <div className="container mx-auto px-4 py-8">
        <ProductDetail />
      </div>
    </>
  );
};
