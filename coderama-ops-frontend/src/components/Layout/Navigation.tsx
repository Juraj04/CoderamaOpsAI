import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';

export const Navigation: React.FC = () => {
  const { user, logout } = useAuth();
  const location = useLocation();

  const isActive = (path: string) => location.pathname === path;

  return (
    <nav className="bg-blue-600 text-white shadow-lg">
      <div className="container mx-auto px-4">
        <div className="flex items-center justify-between h-16">
          {/* Logo */}
          <div className="flex-shrink-0">
            <span className="text-xl font-bold">CoderamaOps</span>
          </div>

          {/* Navigation Links */}
          <div className="flex space-x-4">
            <Link
              to={ROUTES.PRODUCTS}
              className={`px-3 py-2 rounded-md text-sm font-medium transition ${
                isActive(ROUTES.PRODUCTS)
                  ? 'bg-blue-700'
                  : 'hover:bg-blue-500'
              }`}
            >
              Products
            </Link>
            <Link
              to={ROUTES.ORDERS}
              className={`px-3 py-2 rounded-md text-sm font-medium transition ${
                isActive(ROUTES.ORDERS)
                  ? 'bg-blue-700'
                  : 'hover:bg-blue-500'
              }`}
            >
              Orders
            </Link>
          </div>

          {/* User Info & Logout */}
          <div className="flex items-center space-x-4">
            <span className="text-sm">Welcome, {user?.name}</span>
            <button
              onClick={logout}
              className="px-3 py-2 bg-red-500 hover:bg-red-600 rounded-md text-sm font-medium transition"
            >
              Logout
            </button>
          </div>
        </div>
      </div>
    </nav>
  );
};
