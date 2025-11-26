# CoderamaOps Frontend

React + TypeScript frontend for CoderamaOpsAI order management system.

## Prerequisites

- Node.js 20+ and npm
- Backend API running on http://localhost:5000
- Docker (optional, for containerized deployment)

## Installation & Setup

### 1. Install Dependencies
```bash
npm install
```

### 2. Environment Configuration

Create `.env.development` file:
```env
VITE_API_URL=http://localhost:5000
```

### 3. Start Development Server
```bash
npm run dev
```

Application will run on http://localhost:5173 (Vite default port)

### 4. Build for Production
```bash
npm run build
```

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

```
src/
├── api/              # API service layer (axios, endpoints)
├── components/       # React components
├── context/          # React Context (Auth)
├── hooks/            # Custom hooks (useAuth)
├── pages/            # Page components
├── types/            # TypeScript type definitions
└── utils/            # Utilities (storage, constants)
```

## Docker Deployment

### Build and Run with Docker Compose
```bash
# From solution root
docker-compose up -d frontend
```

Frontend will be available at http://localhost:3000

### Manual Docker Build
```bash
# From frontend directory
docker build -t coderama-frontend .
docker run -p 3000:80 coderama-frontend
```

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

```bash
npm run dev          # Start development server
npm run build        # Build for production
npm run preview      # Preview production build locally
npm run lint         # Run ESLint
```

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
