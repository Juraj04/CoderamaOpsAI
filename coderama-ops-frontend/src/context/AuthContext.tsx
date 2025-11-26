import React, { createContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';
import type { User, LoginRequest, LoginResponse } from '../types/auth.types';
import { storage } from '../utils/storage';
import { authApi } from '../api/auth.api';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequest) => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextType>({
  user: null,
  isAuthenticated: false,
  isLoading: true,
  login: async () => {},
  logout: () => {},
});

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Check for existing auth on mount
  useEffect(() => {
    const token = storage.getToken();
    const savedUser = storage.getUser();

    if (token && savedUser && !storage.isTokenExpired()) {
      setUser(savedUser);
    } else {
      storage.clearAuth();
    }

    setIsLoading(false);
  }, []);

  const login = async (credentials: LoginRequest) => {
    const response: LoginResponse = await authApi.login(credentials);

    // Store token and expiry
    storage.setToken(response.token);
    storage.setTokenExpiry(response.expiresAt);

    // Store user info
    const user: User = {
      email: response.email,
      name: response.name,
      userId: response.userId,
    };
    storage.setUser(user);
    setUser(user);
  };

  const logout = () => {
    storage.clearAuth();
    setUser(null);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
