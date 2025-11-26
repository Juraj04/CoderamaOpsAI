## FEATURE:

React frontend application

###
I am not frontend developer, so I cannot specify this app in detail when it comes to coding practice and tehcnologies. I want you to make it simple but correctly and secure.

Add new project in the folder with react app
connect to backend service api methods.

### App features
- login with existing endpoint
    - without login user can not see any other page
- list products, add new product, get product detail on click - only authorized user
- list order - user can see only his orders and not orders of other users.
- create order with some notification that order was ceated
- refresh button so that user can refresh orders list and check the status
- products and orders can be selected from separated top menu bar
- one slide page
- update docker if needs to be

App must be responsive

- must handle Unauthorized response correctly, in case token expires.

check Claude.md and architecture.md for existing implementation.


## EXAMPLES - Codebase References

### Backend API Structure
The React app needs to connect to these existing API endpoints:

**Authentication (CoderamaOpsAI.Api/Controllers/AuthController.cs:33-74)**
- `POST /api/auth/login` - Login endpoint that returns JWT token
  - Request: `{ "email": string, "password": string }`
  - Response: `{ "token": string, "expiresAt": DateTime, "email": string, "name": string }`
  - Token expires in 10 minutes (AuthController.cs:60)

**Products (CoderamaOpsAI.Api/Controllers/ProductsController.cs:11-158)**
- `GET /api/products` - List all products
- `GET /api/products/{id}` - Get product details
- `POST /api/products` - Create new product
- All endpoints require `Authorization: Bearer <token>` header

**Orders (CoderamaOpsAI.Api/Controllers/OrdersController.cs:12-258)**
- `GET /api/orders` - List all orders
- `GET /api/orders/{id}` - Get order details
- `POST /api/orders` - Create new order (publishes OrderCreatedEvent via event bus)
- All endpoints require `Authorization: Bearer <token>` header

### DTO Models Reference

**Login Request/Response (CoderamaOpsAI.Api/Models/LoginRequest.cs, LoginResponse.cs)**
```csharp
// Request
{ email: string, password: string }

// Response
{ token: string, expiresAt: DateTime, email: string, name: string }
```

**Product DTOs (CoderamaOpsAI.Api/Models/ProductDtos.cs:5-46)**
```csharp
// CreateProductRequest
{ name: string (required, max 100 chars),
  description?: string (max 500 chars),
  price: decimal (required, >= 0),
  stock: int (required, >= 0) }

// ProductResponse
{ id: int, name: string, description?: string, price: decimal, stock: int, createdAt: DateTime }
```

**Order DTOs (CoderamaOpsAI.Api/Models/OrderDtos.cs:6-54)**
```csharp
// CreateOrderRequest
{ userId: int (required),
  productId: int (required),
  quantity: int (required, >= 1),
  price: decimal (required, >= 0.01),
  status: OrderStatus (enum) }

// OrderResponse
{ id: int, userId: int, userName: string, productId: int, productName: string,
  quantity: int, price: decimal, total: decimal, status: string,
  createdAt: DateTime, updatedAt: DateTime }
```

### Important Notes from Codebase

**Security & Authentication**
- JWT tokens expire in 10 minutes (CoderamaOpsAI.Api/Controllers/AuthController.cs:60)
- All protected endpoints return 401 Unauthorized if token is missing/expired
- Passwords are hashed with BCrypt (CLAUDE.md:137)
- Generic error messages prevent email enumeration (AuthController.cs:47)

**API Design Conventions (CLAUDE.md:133-134)**
- Lowercase URLs with dash syntax (e.g., `/api/orders` not `/api/Orders`)
- DTOs used for API contracts, never expose entities directly
- All entities have audit fields (CreatedAt, UpdatedAt)

**Order Processing**
- Creating an order publishes OrderCreatedEvent to RabbitMQ (OrdersController.cs:128-133)
- Orders have status changes tracked via background worker
- Users should only see their own orders (requirement to filter by userId)

**CORS & Environment**
- API runs on http://localhost:5000 (docker-compose.yml:7, CLAUDE.md:56)
- Swagger UI available at http://localhost:5000/swagger (CLAUDE.md:57)
- Docker compose includes PostgreSQL (port 5432) and RabbitMQ (ports 5672, 15672)

### Similar Patterns in Project
- Error handling: Global exception middleware used (CLAUDE.md:148-151)
- Validation: DataAnnotations for request validation (see ProductDtos.cs, OrderDtos.cs)
- Testing: Unit tests use xUnit + FluentAssertions + NSubstitute (CLAUDE.md:142-146)

### Edge Cases & Warnings
1. **Token Expiration**: Frontend must handle 401 responses and redirect to login
2. **Order Filtering**: Backend returns ALL orders in GET /api/orders - frontend MUST filter by logged-in user's userId
3. **Price Calculation**: Order total is calculated server-side (OrdersController.cs:110)
4. **Foreign Key Validation**: Backend validates userId and productId exist before creating orders (OrdersController.cs:97-107)


## DOCUMENTATION

### External Resources
- [React Documentation](https://react.dev)
- [JWT Authentication Best Practices](https://auth0.com/blog/refresh-tokens-what-are-they-and-when-to-use-them/)
- [Axios for HTTP requests](https://axios-http.com/docs/intro)

### Internal References
- **CLAUDE.md** - Complete build, run, and coding standards documentation
- **docs/architecture.md** - Full architecture overview including authentication flow
- **README.md:159-173** - Authentication flow example and API usage
- **docker-compose.yml** - Service URLs and environment configuration

### Installation/Run Instructions
Create detailed documentation in the frontend project README including:
1. Prerequisites (Node.js version, npm/yarn)
2. Installation steps (`npm install`)
3. Environment variables setup (.env file with API_URL=http://localhost:5000)
4. Development server command (`npm start`)
5. Build for production (`npm run build`)
6. Docker integration (update docker-compose.yml to include frontend service)
7. How to obtain JWT token for testing (using existing test users from TEST_USERS.md if available)
8. Troubleshooting common issues (CORS, token expiration, connection refused)

Don't forget to describe how to install/run the app on the localhost
