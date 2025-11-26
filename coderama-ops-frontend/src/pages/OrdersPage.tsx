import React, { useState } from 'react';
import { Navigation } from '../components/Layout/Navigation';
import { OrderList } from '../components/Orders/OrderList';
import { OrderForm } from '../components/Orders/OrderForm';

export const OrdersPage: React.FC = () => {
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
        <OrderList key={refreshKey} onCreateClick={() => setShowForm(true)} />
        {showForm && (
          <OrderForm
            onSuccess={handleCreateSuccess}
            onCancel={() => setShowForm(false)}
          />
        )}
      </div>
    </>
  );
};
