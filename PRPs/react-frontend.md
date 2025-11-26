# PRP: React Frontend Application

## 1. Feature Overview

Create a secure, responsive React frontend application that connects to the existing CoderamaOpsAI backend API. The app provides JWT-authenticated access to Products and Orders management with automatic token expiration handling.

### Key Requirements
- Login page with JWT authentication (required before accessing any other features)
- Products management: List all products, create new products, view product details
- Orders management: List user's orders (filtered by userId), create orders, refresh order list
- Single-page application with top navigation menu
- Responsive design (mobile-first approach)
- Automatic handling of token expiration (401 responses → redirect to login)
- Order creation notification
- Docker integration

---

## 2. Backend Context & Existing Implementation

### 2.1 API Endpoints

**Base URL:** `http://localhost:5000`

#### Authentication Endpoint (Unauthorized)
```http
POST /api/auth/login
Content-Type: application/json

Request:
{
  "email": "string",
  "password": "string"
}

Response (200 OK):
{
  "token": "eyJhbGc...",
  "expiresAt": "2024-01-01T12:10:00Z",
  "email": "user@example.com",
  "name": "User Name"
}

Error (401 Unauthorized):
{
  "message": "Invalid email or password"
}
```

**Reference:** `CoderamaOpsAI.Api/Controllers/AuthController.cs:33-74`
**Token Expiration:** 10 minutes (line 60)

#### Products Endpoints (Requires Authorization)
```http
GET /api/products
Authorization: Bearer {token}

Response (200 OK):
[
  {
    "id": 1,
    "name": "Product Name",
    "description": "Optional description",
    "price": 99.99,
    "stock": 100,
    "createdAt": "2024-01-01T12:00:00Z"
  }
]

---

POST /api/products
Authorization: Bearer {token}
Content-Type: application/json

Request:
{
  "name": "Product Name",        // Required, max 100 chars
  "description": "Description",  // Optional, max 500 chars
  "price": 99.99,               // Required, >= 0
  "stock": 100                  // Required, >= 0
}

Response (201 Created): Same as GET response object

---

GET /api/products/{id}
Authorization: Bearer {token}

Response (200 OK): Single product object
Response (404 Not Found): { "message": "Product not found" }
```

**Reference:** `CoderamaOpsAI.Api/Controllers/ProductsController.cs:11-158`
**DTOs:** `CoderamaOpsAI.Api/Models/ProductDtos.cs:5-46`

#### Orders Endpoints (Requires Authorization)
```http
GET /api/orders
Authorization: Bearer {token}

Response (200 OK):
[
  {
    "id": 1,
    "userId": 1,
    "userName": "User Name",
    "productId": 1,
    "productName": "Product Name",
    "quantity": 2,
    "price": 99.99,
    "total": 199.98,
    "status": "Pending",  // Pending | Processing | Completed | Expired
    "createdAt": "2024-01-01T12:00:00Z",
    "updatedAt": "2024-01-01T12:00:00Z"
  }
]

⚠️ CRITICAL: Backend returns ALL orders. Frontend MUST filter by logged-in user's userId.

---

POST /api/orders
Authorization: Bearer {token}
Content-Type: application/json

Request:
{
  "userId": 1,         // Required (use logged-in user's ID)
  "productId": 1,      // Required
  "quantity": 2,       // Required, >= 1
  "price": 99.99,      // Required, >= 0.01
  "status": 0          // Required: 0=Pending, 1=Processing, 2=Completed, 3=Expired
}

Response (201 Created): Same as GET response object
Response (400 Bad Request): Validation errors
Response (404 Not Found): User or Product not found

---

GET /api/orders/{id}
Authorization: Bearer {token}

Response (200 OK): Single order object
Response (404 Not Found): { "message": "Order not found" }
```

**Reference:** `CoderamaOpsAI.Api/Controllers/OrdersController.cs:12-258`
**DTOs:** `CoderamaOpsAI.Api/Models/OrderDtos.cs:6-54`
**OrderStatus Enum:** `CoderamaOpsAI.Dal/Entities/Order.cs:19-25`
  - 0 = Pending
  - 1 = Processing
  - 2 = Completed
  - 3 = Expired

**Event Publishing:** Creating an order publishes `OrderCreatedEvent` to RabbitMQ (OrdersController.cs:128-133)

### 2.2 Test Users

**Reference:** `TEST_USERS.md`

```
User 1 (Admin):
- Email: admin@example.com
- Password: Admin123!
- Name: Admin User

User 2 (Test):
- Email: test@example.com
- Password: Test123!
- Name: Test User
```

### 2.3 Error Handling

**Global Exception Middleware:** `CoderamaOpsAI.Api/Middleware/GlobalExceptionHandlerMiddleware.cs`
- All unhandled exceptions return ProblemDetails JSON
- 401 Unauthorized for missing/expired/invalid JWT tokens
- 400 Bad Request for validation errors (DataAnnotations)
- 404 Not Found for missing resources

### 2.4 Security Considerations

**From CLAUDE.md:136-140**
- Passwords are BCrypt-hashed (never sent to frontend)
- Generic error messages prevent email enumeration
- JWT tokens expire in 10 minutes
- No sensitive data in logs

---

## 3. Critical Implementation Requirements

### 3.1 CORS Configuration (MUST BE ADDED TO BACKEND)

**⚠️ CRITICAL:** The backend currently has NO CORS configuration. This MUST be added before the frontend can connect.

**File to modify:** `CoderamaOpsAI.Api/Program.cs`

**Add after line 15 (after AddControllers):**
```csharp
// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // Vite default port
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

**Add after line 103 (before UseAuthentication):**
```csharp
app.UseCors();
```

**Why:** React dev server (Vite: 5173, CRA: 3000) runs on different port than API (5000), triggering CORS restrictions.

### 3.2 Order Filtering Logic

**⚠️ CRITICAL:** The backend's `GET /api/orders` returns ALL orders for all users.

**Frontend MUST implement client-side filtering:**
```typescript
// After fetching orders from API
const userOrders = allOrders.filter(order => order.userId === currentUser.userId);
```

**Why:** Security requirement - users should only see their own orders (Initials/INITIAL_FE.md:15)

### 3.3 Token Expiration Handling

**⚠️ CRITICAL:** JWT tokens expire after 10 minutes (AuthController.cs:60).

**Frontend MUST:**
1. Store token expiration timestamp from LoginResponse.expiresAt
2. Implement Axios response interceptor to catch 401 responses
3. On 401 response: Clear localStorage, redirect to login, show "Session expired" message
4. Consider warning user 1 minute before expiration

---

## 4. Technology Stack & Architecture

### 4.1 Core Technologies

**Framework:** React 18.3+ with TypeScript
- **Why:** Type safety prevents runtime errors with API contracts
- **Docs:** https://react.dev/learn/typescript

**Build Tool:** Vite 6.0+
- **Why:** Fast HMR, optimized builds, better DX than Create React App
- **Docs:** https://vite.dev/guide/

**Routing:** React Router v6
- **Why:** Standard SPA routing with protected routes
- **Docs:** https://reactrouter.com/en/main

**HTTP Client:** Axios 1.7+
- **Why:** Interceptors for auth headers and error handling
- **Docs:** https://axios-http.com/docs/intro

**State Management:** React Context API
- **Why:** Simple, built-in, sufficient for auth state
- **Alternative:** Zustand if more complex state needed

**Styling:** Tailwind CSS 3+
- **Why:** Utility-first, responsive design out-of-box, fast development
- **Docs:** https://tailwindcss.com/docs/installation
- **Alternative:** CSS Modules if preferred

**UI Components (Optional):** Radix UI or HeadlessUI
- **Why:** Accessible, unstyled components (modals, dropdowns)
- **Docs:** https://www.radix-ui.com/ or https://headlessui.com/

**Notifications:** React Hot Toast
- **Why:** Simple toast notifications for order creation feedback
- **Docs:** https://react-hot-toast.com/

### 4.2 Project Structure

```
coderama-ops-frontend/
├── public/
├── src/
│   ├── api/                    # API service layer
│   │   ├── axiosInstance.ts    # Configured Axios instance
│   │   ├── auth.api.ts         # Auth API calls
│   │   ├── products.api.ts     # Products API calls
│   │   └── orders.api.ts       # Orders API calls
│   ├── components/             # React components
│   │   ├── Layout/
│   │   │   ├── Navigation.tsx  # Top menu bar
│   │   │   └── ProtectedRoute.tsx
│   │   ├── Auth/
│   │   │   └── LoginForm.tsx
│   │   ├── Products/
│   │   │   ├── ProductList.tsx
│   │   │   ├── ProductDetail.tsx
│   │   │   └── ProductForm.tsx
│   │   └── Orders/
│   │       ├── OrderList.tsx
│   │       └── OrderForm.tsx
│   ├── context/
│   │   └── AuthContext.tsx     # Auth state management
│   ├── hooks/
│   │   └── useAuth.ts          # Custom auth hook
│   ├── types/
│   │   ├── auth.types.ts       # LoginRequest, LoginResponse
│   │   ├── product.types.ts    # Product DTOs
│   │   └── order.types.ts      # Order DTOs, OrderStatus enum
│   ├── pages/
│   │   ├── LoginPage.tsx
│   │   ├── ProductsPage.tsx
│   │   └── OrdersPage.tsx
│   ├── utils/
│   │   ├── storage.ts          # localStorage helpers
│   │   └── constants.ts        # API_URL, etc.
│   ├── App.tsx
│   ├── main.tsx
│   └── index.css
├── .env.development
├── .env.production
├── Dockerfile
├── .dockerignore
├── package.json
├── tsconfig.json
├── vite.config.ts
├── tailwind.config.js
└── README.md
```

### 4.3 Data Flow Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      React Application                       │
│                                                               │
│  ┌────────────┐    ┌──────────────┐    ┌─────────────┐     │
│  │   Pages    │───▶│  Components  │───▶│   Context   │     │
│  │            │    │              │    │  (Auth)     │     │
│  │ - Login    │    │ - LoginForm  │    │             │     │
│  │ - Products │    │ - ProductList│    │ - user      │     │
│  │ - Orders   │    │ - OrderList  │    │ - token     │     │
│  └────────────┘    └──────────────┘    │ - logout()  │     │
│         │                  │             └─────────────┘     │
│         └──────────────────┘                    │            │
│                      │                           │            │
│                      ▼                           ▼            │
│            ┌─────────────────────────────────────────┐       │
│            │        API Service Layer                │       │
│            │                                         │       │
│            │  ┌─────────────────────────────────┐   │       │
│            │  │   Axios Instance (Interceptors)  │   │       │
│            │  │                                   │   │       │
│            │  │  Request Interceptor:            │   │       │
│            │  │  - Add Authorization header      │   │       │
│            │  │                                   │   │       │
│            │  │  Response Interceptor:           │   │       │
│            │  │  - Catch 401 → logout + redirect │   │       │
│            │  └─────────────────────────────────┘   │       │
│            │                                         │       │
│            │  API Modules:                           │       │
│            │  - auth.api.ts (login)                  │       │
│            │  - products.api.ts (CRUD)               │       │
│            │  - orders.api.ts (CRUD + filter)        │       │
│            └─────────────────────────────────────────┘       │
│                              │                                │
└──────────────────────────────┼────────────────────────────────┘
                               │ HTTP Requests
                               ▼
                    ┌─────────────────────┐
                    │   Backend API       │
                    │   localhost:5000    │
                    │                     │
                    │  /api/auth/login    │
                    │  /api/products      │
                    │  /api/orders        │
                    └─────────────────────┘
```

---

## 5. Implementation Plan (Step-by-Step)

### Phase 1: Backend Preparation

#### Task 1.1: Add CORS Configuration
- **File:** `CoderamaOpsAI.Api/Program.cs`
- **Action:** Add CORS policy as specified in section 3.1
- **Test:** Run `dotnet build CoderamaOpsAI.sln` - should compile without errors

#### Task 1.2: Verify API Functionality
- **Action:** Start API with `dotnet run --project CoderamaOpsAI.Api`
- **Test:** Use Swagger UI (http://localhost:5000/swagger) to:
  - Login with test user (admin@example.com / Admin123!)
  - Create a product
  - Create an order
  - Verify responses match DTOs in section 2.1

---

### Phase 2: Frontend Project Setup

#### Task 2.1: Initialize Vite + React + TypeScript Project
```bash
# Run from solution root
npm create vite@latest coderama-ops-frontend -- --template react-ts
cd coderama-ops-frontend
npm install
```

#### Task 2.2: Install Dependencies
```bash
npm install react-router-dom axios react-hot-toast
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
```

#### Task 2.3: Configure Tailwind CSS
- **File:** `tailwind.config.js`
```javascript
/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}
```

- **File:** `src/index.css`
```css
@tailwind base;
@tailwind components;
@tailwind utilities;
```

#### Task 2.4: Setup Environment Variables
- **File:** `.env.development`
```env
VITE_API_URL=http://localhost:5000
```

- **File:** `.env.production`
```env
VITE_API_URL=http://localhost:5000
```

#### Task 2.5: Create .gitignore Additions
```
# Frontend specific
node_modules/
dist/
.env.local
.DS_Store
```

---

### Phase 3: Core Infrastructure

#### Task 3.1: Define TypeScript Types
- **File:** `src/types/auth.types.ts`
```typescript
export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string; // ISO 8601 date string
  email: string;
  name: string;
}

export interface User {
  email: string;
  name: string;
  userId?: number; // Will be extracted from JWT or stored separately
}
```

- **File:** `src/types/product.types.ts`
```typescript
export interface Product {
  id: number;
  name: string;
  description?: string;
  price: number;
  stock: number;
  createdAt: string;
}

export interface CreateProductRequest {
  name: string;
  description?: string;
  price: number;
  stock: number;
}
```

- **File:** `src/types/order.types.ts`
```typescript
export enum OrderStatus {
  Pending = 0,
  Processing = 1,
  Completed = 2,
  Expired = 3
}

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
  status: OrderStatus;
}
```

#### Task 3.2: Create Constants and Utils
- **File:** `src/utils/constants.ts`
```typescript
export const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export const STORAGE_KEYS = {
  AUTH_TOKEN: 'auth_token',
  AUTH_USER: 'auth_user',
  TOKEN_EXPIRY: 'token_expiry',
} as const;

export const ROUTES = {
  LOGIN: '/login',
  PRODUCTS: '/products',
  ORDERS: '/orders',
  PRODUCT_DETAIL: '/products/:id',
} as const;
```

- **File:** `src/utils/storage.ts`
```typescript
import { STORAGE_KEYS } from './constants';
import { User } from '../types/auth.types';

export const storage = {
  setToken: (token: string) => {
    localStorage.setItem(STORAGE_KEYS.AUTH_TOKEN, token);
  },

  getToken: (): string | null => {
    return localStorage.getItem(STORAGE_KEYS.AUTH_TOKEN);
  },

  setUser: (user: User) => {
    localStorage.setItem(STORAGE_KEYS.AUTH_USER, JSON.stringify(user));
  },

  getUser: (): User | null => {
    const userStr = localStorage.getItem(STORAGE_KEYS.AUTH_USER);
    return userStr ? JSON.parse(userStr) : null;
  },

  setTokenExpiry: (expiresAt: string) => {
    localStorage.setItem(STORAGE_KEYS.TOKEN_EXPIRY, expiresAt);
  },

  getTokenExpiry: (): string | null => {
    return localStorage.getItem(STORAGE_KEYS.TOKEN_EXPIRY);
  },

  isTokenExpired: (): boolean => {
    const expiry = storage.getTokenExpiry();
    if (!expiry) return true;
    return new Date(expiry) <= new Date();
  },

  clearAuth: () => {
    localStorage.removeItem(STORAGE_KEYS.AUTH_TOKEN);
    localStorage.removeItem(STORAGE_KEYS.AUTH_USER);
    localStorage.removeItem(STORAGE_KEYS.TOKEN_EXPIRY);
  },
};
```

#### Task 3.3: Configure Axios Instance
- **File:** `src/api/axiosInstance.ts`
```typescript
import axios from 'axios';
import { API_URL, ROUTES } from '../utils/constants';
import { storage } from '../utils/storage';

// Create axios instance
export const axiosInstance = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor - add auth token
axiosInstance.interceptors.request.use(
  (config) => {
    const token = storage.getToken();

    // Check if token is expired before making request
    if (token && storage.isTokenExpired()) {
      storage.clearAuth();
      window.location.href = ROUTES.LOGIN;
      return Promise.reject(new Error('Token expired'));
    }

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor - handle 401 errors
axiosInstance.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Token is invalid or expired
      storage.clearAuth();

      // Only redirect if not already on login page
      if (window.location.pathname !== ROUTES.LOGIN) {
        window.location.href = ROUTES.LOGIN;
        // Optionally show toast notification
        // toast.error('Session expired. Please login again.');
      }
    }

    return Promise.reject(error);
  }
);
```

#### Task 3.4: Create API Service Modules
- **File:** `src/api/auth.api.ts`
```typescript
import { axiosInstance } from './axiosInstance';
import { LoginRequest, LoginResponse } from '../types/auth.types';

export const authApi = {
  login: async (credentials: LoginRequest): Promise<LoginResponse> => {
    const response = await axiosInstance.post<LoginResponse>(
      '/api/auth/login',
      credentials
    );
    return response.data;
  },
};
```

- **File:** `src/api/products.api.ts`
```typescript
import { axiosInstance } from './axiosInstance';
import { Product, CreateProductRequest } from '../types/product.types';

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
```

- **File:** `src/api/orders.api.ts`
```typescript
import { axiosInstance } from './axiosInstance';
import { Order, CreateOrderRequest } from '../types/order.types';

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
```

---

### Phase 4: Authentication Context & Protected Routes

#### Task 4.1: Create Auth Context
- **File:** `src/context/AuthContext.tsx`
```typescript
import React, { createContext, useState, useEffect, ReactNode } from 'react';
import { User, LoginRequest, LoginResponse } from '../types/auth.types';
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
```

#### Task 4.2: Create Custom Auth Hook
- **File:** `src/hooks/useAuth.ts`
```typescript
import { useContext } from 'react';
import { AuthContext } from '../context/AuthContext';

export const useAuth = () => {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }

  return context;
};
```

#### Task 4.3: Create Protected Route Component
- **File:** `src/components/Layout/ProtectedRoute.tsx`
```typescript
import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';

interface ProtectedRouteProps {
  children: React.ReactNode;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children }) => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-lg">Loading...</div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to={ROUTES.LOGIN} replace />;
  }

  return <>{children}</>;
};
```

---

### Phase 5: UI Components

#### Task 5.1: Create Navigation Component
- **File:** `src/components/Layout/Navigation.tsx`
```typescript
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
```

#### Task 5.2: Create Login Form Component
- **File:** `src/components/Auth/LoginForm.tsx`
```typescript
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';
import toast from 'react-hot-toast';

export const LoginForm: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);

    try {
      await login({ email, password });
      toast.success('Login successful!');
      navigate(ROUTES.PRODUCTS);
    } catch (error: any) {
      const message = error.response?.data?.message || 'Login failed. Please check your credentials.';
      toast.error(message);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100">
      <div className="bg-white p-8 rounded-lg shadow-md w-full max-w-md">
        <h2 className="text-2xl font-bold text-center mb-6">Login</h2>

        {/* Test Credentials Info */}
        <div className="mb-4 p-3 bg-blue-50 border border-blue-200 rounded text-sm">
          <p className="font-semibold mb-1">Test Credentials:</p>
          <p>Email: admin@example.com</p>
          <p>Password: Admin123!</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
              Email
            </label>
            <input
              id="email"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="Enter your email"
            />
          </div>

          <div>
            <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
              Password
            </label>
            <input
              id="password"
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="Enter your password"
            />
          </div>

          <button
            type="submit"
            disabled={isLoading}
            className="w-full py-2 px-4 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-md transition disabled:bg-gray-400"
          >
            {isLoading ? 'Logging in...' : 'Login'}
          </button>
        </form>
      </div>
    </div>
  );
};
```

#### Task 5.3: Create Product Components
- **File:** `src/components/Products/ProductList.tsx`
```typescript
import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { productsApi } from '../../api/products.api';
import { Product } from '../../types/product.types';
import toast from 'react-hot-toast';

interface ProductListProps {
  onCreateClick: () => void;
}

export const ProductList: React.FC<ProductListProps> = ({ onCreateClick }) => {
  const [products, setProducts] = useState<Product[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const fetchProducts = async () => {
    try {
      setIsLoading(true);
      const data = await productsApi.getAll();
      setProducts(data);
    } catch (error) {
      toast.error('Failed to load products');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchProducts();
  }, []);

  if (isLoading) {
    return <div className="text-center py-8">Loading products...</div>;
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold">Products</h2>
        <button
          onClick={onCreateClick}
          className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-md transition"
        >
          Add Product
        </button>
      </div>

      {products.length === 0 ? (
        <div className="text-center py-8 text-gray-500">
          No products found. Create your first product!
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {products.map((product) => (
            <Link
              key={product.id}
              to={`/products/${product.id}`}
              className="bg-white p-6 rounded-lg shadow hover:shadow-lg transition"
            >
              <h3 className="text-xl font-semibold mb-2">{product.name}</h3>
              <p className="text-gray-600 text-sm mb-4">
                {product.description || 'No description'}
              </p>
              <div className="flex justify-between items-center">
                <span className="text-2xl font-bold text-blue-600">
                  ${product.price.toFixed(2)}
                </span>
                <span className="text-sm text-gray-500">
                  Stock: {product.stock}
                </span>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
};
```

- **File:** `src/components/Products/ProductForm.tsx`
```typescript
import React, { useState } from 'react';
import { productsApi } from '../../api/products.api';
import { CreateProductRequest } from '../../types/product.types';
import toast from 'react-hot-toast';

interface ProductFormProps {
  onSuccess: () => void;
  onCancel: () => void;
}

export const ProductForm: React.FC<ProductFormProps> = ({ onSuccess, onCancel }) => {
  const [formData, setFormData] = useState<CreateProductRequest>({
    name: '',
    description: '',
    price: 0,
    stock: 0,
  });
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: name === 'price' || name === 'stock' ? parseFloat(value) || 0 : value,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Validation
    if (formData.price < 0) {
      toast.error('Price must be >= 0');
      return;
    }
    if (formData.stock < 0) {
      toast.error('Stock must be >= 0');
      return;
    }

    setIsSubmitting(true);

    try {
      await productsApi.create(formData);
      toast.success('Product created successfully!');
      onSuccess();
    } catch (error: any) {
      const message = error.response?.data?.message || 'Failed to create product';
      toast.error(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg p-6 max-w-md w-full">
        <h2 className="text-2xl font-bold mb-4">Add New Product</h2>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Name *
            </label>
            <input
              type="text"
              name="name"
              required
              maxLength={100}
              value={formData.name}
              onChange={handleChange}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Description
            </label>
            <textarea
              name="description"
              maxLength={500}
              value={formData.description}
              onChange={handleChange}
              rows={3}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Price * (min: 0)
            </label>
            <input
              type="number"
              name="price"
              required
              min="0"
              step="0.01"
              value={formData.price}
              onChange={handleChange}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Stock * (min: 0)
            </label>
            <input
              type="number"
              name="stock"
              required
              min="0"
              value={formData.stock}
              onChange={handleChange}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div className="flex space-x-3 pt-4">
            <button
              type="submit"
              disabled={isSubmitting}
              className="flex-1 py-2 px-4 bg-blue-600 hover:bg-blue-700 text-white rounded-md transition disabled:bg-gray-400"
            >
              {isSubmitting ? 'Creating...' : 'Create'}
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
```

- **File:** `src/components/Products/ProductDetail.tsx`
```typescript
import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { productsApi } from '../../api/products.api';
import { Product } from '../../types/product.types';
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
```

#### Task 5.4: Create Order Components
- **File:** `src/components/Orders/OrderList.tsx`
```typescript
import React, { useState, useEffect } from 'react';
import { ordersApi } from '../../api/orders.api';
import { Order } from '../../types/order.types';
import { storage } from '../../utils/storage';
import toast from 'react-hot-toast';

interface OrderListProps {
  onCreateClick: () => void;
}

export const OrderList: React.FC<OrderListProps> = ({ onCreateClick }) => {
  const [orders, setOrders] = useState<Order[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const fetchOrders = async () => {
    try {
      setIsLoading(true);

      // CRITICAL: Get current user to filter orders
      const user = storage.getUser();
      if (!user) {
        toast.error('User not found');
        return;
      }

      // CRITICAL: Filter orders by userId (backend returns ALL orders)
      // See section 3.2 for explanation
      const allOrders = await ordersApi.getAll();

      // TODO: Extract userId from JWT token or store in user object during login
      // For now, we'll need to add userId to the User type and store it during login
      // const userOrders = allOrders.filter(order => order.userId === user.userId);

      // TEMPORARY: Show all orders (MUST BE FIXED)
      // In production, you MUST filter by userId
      setOrders(allOrders);

      toast.info('Note: Currently showing all orders. Production must filter by userId.');
    } catch (error) {
      toast.error('Failed to load orders');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchOrders();
  }, []);

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'pending':
        return 'bg-yellow-100 text-yellow-800';
      case 'processing':
        return 'bg-blue-100 text-blue-800';
      case 'completed':
        return 'bg-green-100 text-green-800';
      case 'expired':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  if (isLoading) {
    return <div className="text-center py-8">Loading orders...</div>;
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold">My Orders</h2>
        <div className="space-x-3">
          <button
            onClick={fetchOrders}
            className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-md transition"
          >
            🔄 Refresh
          </button>
          <button
            onClick={onCreateClick}
            className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-md transition"
          >
            Create Order
          </button>
        </div>
      </div>

      {orders.length === 0 ? (
        <div className="text-center py-8 text-gray-500">
          No orders found. Create your first order!
        </div>
      ) : (
        <div className="bg-white rounded-lg shadow overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Order ID
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Product
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Quantity
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Total
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Status
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Date
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {orders.map((order) => (
                <tr key={order.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    #{order.id}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {order.productName}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {order.quantity}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-semibold text-gray-900">
                    ${order.total.toFixed(2)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${getStatusColor(order.status)}`}>
                      {order.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {new Date(order.createdAt).toLocaleDateString()}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
```

- **File:** `src/components/Orders/OrderForm.tsx`
```typescript
import React, { useState, useEffect } from 'react';
import { ordersApi } from '../../api/orders.api';
import { productsApi } from '../../api/products.api';
import { CreateOrderRequest, OrderStatus } from '../../types/order.types';
import { Product } from '../../types/product.types';
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

    // TODO: Get userId from JWT token or store in user object during login
    // For now, we'll use a placeholder (MUST BE FIXED in production)
    // const userId = user.userId;
    const userId = 1; // TEMPORARY PLACEHOLDER

    const orderRequest: CreateOrderRequest = {
      userId: userId,
      productId: selectedProduct.id,
      quantity: quantity,
      price: selectedProduct.price,
      status: OrderStatus.Pending,
    };

    setIsSubmitting(true);

    try {
      await ordersApi.create(orderRequest);

      // Show success notification (requirement from section 1)
      toast.success('Order created successfully! 🎉');

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
```

---

### Phase 6: Pages & Routing

#### Task 6.1: Create Page Components
- **File:** `src/pages/LoginPage.tsx`
```typescript
import React from 'react';
import { LoginForm } from '../components/Auth/LoginForm';

export const LoginPage: React.FC = () => {
  return <LoginForm />;
};
```

- **File:** `src/pages/ProductsPage.tsx`
```typescript
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
```

- **File:** `src/pages/OrdersPage.tsx`
```typescript
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
```

- **File:** `src/pages/ProductDetailPage.tsx`
```typescript
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
```

#### Task 6.2: Setup App Routing
- **File:** `src/App.tsx`
```typescript
import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { AuthProvider } from './context/AuthContext';
import { ProtectedRoute } from './components/Layout/ProtectedRoute';
import { LoginPage } from './pages/LoginPage';
import { ProductsPage } from './pages/ProductsPage';
import { ProductDetailPage } from './pages/ProductDetailPage';
import { OrdersPage } from './pages/OrdersPage';
import { ROUTES } from './utils/constants';

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Toaster position="top-right" />
        <Routes>
          {/* Public Routes */}
          <Route path={ROUTES.LOGIN} element={<LoginPage />} />

          {/* Protected Routes */}
          <Route
            path={ROUTES.PRODUCTS}
            element={
              <ProtectedRoute>
                <ProductsPage />
              </ProtectedRoute>
            }
          />
          <Route
            path={ROUTES.PRODUCT_DETAIL}
            element={
              <ProtectedRoute>
                <ProductDetailPage />
              </ProtectedRoute>
            }
          />
          <Route
            path={ROUTES.ORDERS}
            element={
              <ProtectedRoute>
                <OrdersPage />
              </ProtectedRoute>
            }
          />

          {/* Default Route */}
          <Route path="/" element={<Navigate to={ROUTES.PRODUCTS} replace />} />

          {/* 404 Route */}
          <Route path="*" element={<Navigate to={ROUTES.PRODUCTS} replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
```

- **File:** `src/main.tsx`
```typescript
import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import './index.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
```

---

### Phase 7: Docker Integration

#### Task 7.1: Create Frontend Dockerfile
- **File:** `coderama-ops-frontend/Dockerfile`
```dockerfile
# Build stage
FROM node:20-alpine AS build

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Build the application
RUN npm run build

# Production stage
FROM nginx:1.25-alpine

# Copy built files
COPY --from=build /app/dist /usr/share/nginx/html

# Copy custom nginx config (optional - see below)
# COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose port 80
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
```

#### Task 7.2: Create .dockerignore
- **File:** `coderama-ops-frontend/.dockerignore`
```
node_modules
dist
.git
.env.local
.DS_Store
npm-debug.log*
```

#### Task 7.3: Update docker-compose.yml
- **File:** `docker-compose.yml` (add frontend service)

**Add after the `worker` service:**
```yaml
  frontend:
    build:
      context: ./coderama-ops-frontend
      dockerfile: Dockerfile
    ports:
      - "3000:80"
    environment:
      - VITE_API_URL=http://localhost:5000
    depends_on:
      - api
    restart: unless-stopped
```

**Note:** The frontend will be accessible at http://localhost:3000

#### Task 7.4: Create nginx.conf (Optional - for SPA routing)
- **File:** `coderama-ops-frontend/nginx.conf`
```nginx
server {
    listen 80;
    server_name localhost;
    root /usr/share/nginx/html;
    index index.html;

    # SPA fallback - serve index.html for all routes
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Cache static assets
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

**To use this config, uncomment the COPY line in Dockerfile (Task 7.1)**

---

### Phase 8: Documentation

#### Task 8.1: Create Frontend README
- **File:** `coderama-ops-frontend/README.md`

```markdown
# CoderamaOps Frontend

React + TypeScript frontend for CoderamaOpsAI order management system.

## Prerequisites

- Node.js 20+ and npm
- Backend API running on http://localhost:5000
- Docker (optional, for containerized deployment)

## Installation & Setup

### 1. Install Dependencies
\```bash
npm install
\```

### 2. Environment Configuration

Create `.env.development` file:
\```env
VITE_API_URL=http://localhost:5000
\```

### 3. Start Development Server
\```bash
npm run dev
\```

Application will run on http://localhost:5173 (Vite default port)

### 4. Build for Production
\```bash
npm run build
\```

Built files will be in the `dist/` directory.

## Test Credentials

**Admin User:**
- Email: `admin@example.com`
- Password: `Admin123!`

**Test User:**
- Email: `test@example.com`
- Password: `Test123!`

## Features

- **Authentication**: JWT-based login with 10-minute token expiration
- **Products Management**: List, create, and view product details
- **Orders Management**: List user orders, create orders, refresh status
- **Responsive Design**: Mobile-first, works on all screen sizes
- **Auto Token Refresh**: Automatic logout on token expiration
- **Toast Notifications**: Real-time feedback for user actions

## Project Structure

\```
src/
├── api/              # API service layer (axios, endpoints)
├── components/       # React components
├── context/          # React Context (Auth)
├── hooks/            # Custom hooks (useAuth)
├── pages/            # Page components
├── types/            # TypeScript type definitions
└── utils/            # Utilities (storage, constants)
\```

## Docker Deployment

### Build and Run with Docker Compose
\```bash
# From solution root
docker-compose up -d frontend
\```

Frontend will be available at http://localhost:3000

### Manual Docker Build
\```bash
# From frontend directory
docker build -t coderama-frontend .
docker run -p 3000:80 coderama-frontend
\```

## Troubleshooting

### CORS Errors
**Issue:** Requests to backend fail with CORS errors
**Solution:** Ensure backend has CORS configured (see `CoderamaOpsAI.Api/Program.cs`)

### Token Expiration
**Issue:** Getting logged out unexpectedly
**Solution:** JWT tokens expire after 10 minutes. This is expected behavior.

### Cannot Connect to Backend
**Issue:** API calls fail with connection refused
**Solution:**
1. Verify backend is running: `docker-compose ps`
2. Check backend URL in `.env.development` matches backend port
3. Try accessing http://localhost:5000/swagger to verify backend is up

### Orders Not Showing
**Issue:** Orders page is empty but orders exist in database
**Solution:** Current implementation shows all orders. Check console for errors.

## Development Notes

### Critical TODOs for Production

1. **User ID Extraction:** Currently using placeholder userId=1 when creating orders. MUST extract userId from JWT token or store in auth context during login.

2. **Order Filtering:** Currently showing all orders. MUST filter orders by logged-in user's userId in production.

3. **Security:** Consider using HttpOnly cookies instead of localStorage for JWT tokens to prevent XSS attacks.

4. **Error Handling:** Add more specific error messages for different failure scenarios.

5. **Loading States:** Add skeleton loaders for better UX during data fetching.

## Scripts

\```bash
npm run dev          # Start development server
npm run build        # Build for production
npm run preview      # Preview production build locally
npm run lint         # Run ESLint
\```

## API Endpoints

All endpoints require `Authorization: Bearer <token>` header except login.

- `POST /api/auth/login` - User login
- `GET /api/products` - List all products
- `POST /api/products` - Create product
- `GET /api/products/{id}` - Get product details
- `GET /api/orders` - List all orders
- `POST /api/orders` - Create order
- `GET /api/orders/{id}` - Get order details

## Technologies

- **React 18** - UI library
- **TypeScript** - Type safety
- **Vite** - Build tool
- **React Router v6** - Routing
- **Axios** - HTTP client
- **Tailwind CSS** - Styling
- **React Hot Toast** - Notifications

## License

MIT
\```

---

## 6. Validation Gates (MUST PASS)

### Gate 1: Backend CORS Configuration
```bash
# From solution root
dotnet build CoderamaOpsAI.sln
```
**Expected:** Build succeeds with no errors

### Gate 2: Backend API Running
```bash
docker-compose up -d api db rabbitmq
```
**Expected:**
- API accessible at http://localhost:5000/swagger
- Login endpoint returns JWT token
- Products/Orders endpoints return 401 without token

### Gate 3: Frontend Build
```bash
cd coderama-ops-frontend
npm install
npm run build
```
**Expected:**
- All dependencies install successfully
- Build completes without TypeScript errors
- `dist/` folder created with built files

### Gate 4: Frontend Development Server
```bash
cd coderama-ops-frontend
npm run dev
```
**Expected:**
- Dev server starts on http://localhost:5173
- No console errors on page load
- Login page displays

### Gate 5: End-to-End User Flow
**Manual Test:**
1. Open http://localhost:5173
2. Login with `admin@example.com` / `Admin123!`
3. Verify redirect to Products page
4. Create a new product
5. Navigate to Orders page
6. Create an order (select the product created in step 4)
7. Verify toast notification appears
8. Click Refresh button
9. Verify order appears in list
10. Click Logout
11. Verify redirect to login page

**Expected:** All steps complete without errors

### Gate 6: Docker Build
```bash
# From solution root
docker-compose up -d --build
```
**Expected:**
- All services start successfully
- Frontend accessible at http://localhost:3000
- Backend accessible at http://localhost:5000
- No container errors in logs

### Gate 7: Responsive Design Test
**Manual Test:**
1. Open frontend in browser
2. Open DevTools (F12)
3. Toggle device toolbar (Ctrl+Shift+M)
4. Test with:
   - iPhone SE (375px)
   - iPad (768px)
   - Desktop (1920px)

**Expected:** All pages render correctly at all breakpoints

---

## 7. Known Issues & Future Improvements

### Critical Issues to Fix Before Production

1. **User ID Management (HIGH PRIORITY)**
   - Current: Using placeholder `userId = 1` in OrderForm
   - Required: Extract `userId` from JWT token claims or store during login
   - Location: `src/components/Orders/OrderForm.tsx:65`

2. **Order Filtering (HIGH PRIORITY)**
   - Current: Showing all orders from backend
   - Required: Filter by logged-in user's `userId`
   - Location: `src/components/Orders/OrderList.tsx:33`

3. **JWT Token Storage (SECURITY)**
   - Current: Storing JWT in localStorage (vulnerable to XSS)
   - Recommended: Use HttpOnly cookies for better security
   - Reference: https://auth0.com/docs/secure/security-guidance/data-security/token-storage

### Nice-to-Have Improvements

4. **Token Refresh Mechanism**
   - Add refresh token endpoint to backend
   - Implement auto-refresh before token expires
   - Show countdown warning 1 minute before expiration

5. **Loading Skeletons**
   - Replace "Loading..." text with skeleton components
   - Better UX during data fetching

6. **Form Validation**
   - Add client-side validation with react-hook-form
   - Show field-level error messages

7. **Pagination**
   - Add pagination to Products and Orders lists
   - Backend support may be required

8. **Search & Filters**
   - Add search bar for products
   - Filter orders by status, date range

9. **Optimistic Updates**
   - Update UI immediately on create/update
   - Rollback on error

10. **Unit & Integration Tests**
    - Add Vitest for component tests
    - Add React Testing Library for integration tests
    - Add Playwright for E2E tests

---

## 8. External Resources & Documentation

### Official Documentation
- **React:** https://react.dev/learn
- **TypeScript:** https://www.typescriptlang.org/docs/
- **Vite:** https://vite.dev/guide/
- **React Router:** https://reactrouter.com/en/main
- **Axios:** https://axios-http.com/docs/intro
- **Tailwind CSS:** https://tailwindcss.com/docs/installation
- **React Hot Toast:** https://react-hot-toast.com/docs

### Security Best Practices
- **JWT Best Practices:** https://auth0.com/blog/a-look-at-the-latest-draft-for-jwt-bcp/
- **OWASP Top 10:** https://owasp.org/www-project-top-ten/
- **Token Storage:** https://auth0.com/docs/secure/security-guidance/data-security/token-storage

### Responsive Design
- **Tailwind Responsive Design:** https://tailwindcss.com/docs/responsive-design
- **CSS Media Queries:** https://developer.mozilla.org/en-US/docs/Web/CSS/Media_Queries/Using_media_queries

### TypeScript Patterns
- **React TypeScript Cheatsheet:** https://react-typescript-cheatsheet.netlify.app/

---

## 9. Quality Score & Confidence Assessment

### Complexity Analysis

**Frontend Complexity:** Medium
- Standard React patterns (Context, hooks, routing)
- Well-documented API contracts
- Clear type definitions provided

**Backend Modifications:** Low
- Only CORS configuration needed
- No breaking changes to existing code

**Integration Points:** 3
1. Authentication flow (JWT)
2. Products CRUD operations
3. Orders CRUD operations

### Risk Assessment

**High Risk Areas:**
- ❌ User ID extraction from JWT (currently placeholder)
- ❌ Order filtering logic (currently shows all orders)

**Medium Risk Areas:**
- ⚠️ Token expiration handling (10-minute window is short)
- ⚠️ CORS configuration (must be added correctly)

**Low Risk Areas:**
- ✅ API contract matching (DTOs clearly defined)
- ✅ Build tooling (Vite is stable)
- ✅ UI components (standard patterns)

### Validation Coverage

- ✅ Build validation (dotnet build, npm build)
- ✅ Runtime validation (dev server, docker)
- ✅ Manual E2E testing (user flow walkthrough)
- ❌ Automated tests (not included - future improvement)

### Documentation Quality

- ✅ Step-by-step implementation guide
- ✅ Code examples with inline comments
- ✅ Troubleshooting section
- ✅ External resource links
- ✅ Known issues documented
- ✅ Future improvements listed

### PRP Score: **7.5/10**

**Confidence Level for One-Pass Implementation:**

**Strengths:**
- Comprehensive API documentation with exact DTOs
- Clear step-by-step implementation plan
- All critical issues documented upfront
- Validation gates ensure correctness at each phase
- Working test users provided
- CORS fix documented clearly

**Weaknesses:**
- User ID extraction requires JWT parsing (not fully specified)
- Order filtering requires knowing user's ID (not in LoginResponse)
- No automated tests included
- Security improvements (HttpOnly cookies) not implemented

**Recommendation:**
Agent should be able to complete 80-90% of implementation in one pass. The two critical issues (user ID management and order filtering) will require either:
1. Backend modification to include `userId` in LoginResponse
2. JWT token parsing on frontend to extract user ID from claims

**Suggested Pre-Implementation Clarification:**
Ask user: "Should I modify the backend to include userId in LoginResponse, or should I parse the JWT token on the frontend to extract the user ID?"

---

## 10. Implementation Checklist

Use this checklist during implementation:

### Backend Preparation
- [ ] Add CORS configuration to Program.cs
- [ ] Build solution successfully
- [ ] Start API and verify Swagger UI accessible
- [ ] Test login endpoint with test credentials
- [ ] Verify products and orders endpoints require auth

### Frontend Setup
- [ ] Create Vite + React + TypeScript project
- [ ] Install all dependencies (axios, react-router-dom, tailwind, toast)
- [ ] Configure Tailwind CSS
- [ ] Create .env.development file
- [ ] Setup .gitignore

### Core Infrastructure
- [ ] Define TypeScript types (auth, product, order)
- [ ] Create constants and utils
- [ ] Configure axios instance with interceptors
- [ ] Create API service modules (auth, products, orders)
- [ ] Test API calls with Postman/Swagger first

### Authentication
- [ ] Create AuthContext
- [ ] Create useAuth hook
- [ ] Create ProtectedRoute component
- [ ] Test token storage and retrieval
- [ ] Test 401 interceptor (expire token manually)

### UI Components
- [ ] Create Navigation component
- [ ] Create LoginForm component
- [ ] Create ProductList component
- [ ] Create ProductForm component
- [ ] Create ProductDetail component
- [ ] Create OrderList component
- [ ] Create OrderForm component
- [ ] Test all components in isolation

### Pages & Routing
- [ ] Create page components
- [ ] Setup routing in App.tsx
- [ ] Test navigation between pages
- [ ] Test protected routes (redirect to login)

### Docker Integration
- [ ] Create frontend Dockerfile
- [ ] Create .dockerignore
- [ ] Update docker-compose.yml
- [ ] Build and test Docker image
- [ ] Test full docker-compose stack

### Documentation
- [ ] Create frontend README.md
- [ ] Document test credentials
- [ ] Document troubleshooting steps
- [ ] Document known issues

### Testing
- [ ] Run all validation gates
- [ ] Test responsive design at multiple breakpoints
- [ ] Test token expiration flow
- [ ] Test order creation with notification
- [ ] Test refresh button on orders page

### Final Review
- [ ] No console errors in browser
- [ ] No TypeScript compilation errors
- [ ] All features working as specified
- [ ] Docker builds successfully
- [ ] Documentation complete

---

## End of PRP
