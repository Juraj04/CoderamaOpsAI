import React, { useState } from 'react';
import { Navigation } from '../components/Layout/Navigation';
import { ProductList } from '../components/Products/ProductList';
import { ProductForm } from '../components/Products/ProductForm';

export const ProductsPage: React.FC = () => {
  const [showForm, setShowForm] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);

  const handleCreateSuccess = () => {
    setShowForm(false);
    setRefreshKey(prev => prev + 1); // Trigger re-fetch
  };

  return (
    <>
      <Navigation />
      <div className="container mx-auto px-4 py-8">
        <ProductList key={refreshKey} onCreateClick={() => setShowForm(true)} />
        {showForm && (
          <ProductForm
            onSuccess={handleCreateSuccess}
            onCancel={() => setShowForm(false)}
          />
        )}
      </div>
    </>
  );
};
