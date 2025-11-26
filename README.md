# CoderamaOpsAI

A full-stack order management system with .NET 8 backend and React frontend, featuring event-driven architecture.

## Table of Contents
- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Project Structure](#project-structure)
- [Running Tests](#running-tests)
- [API Documentation](#api-documentation)
- [Troubleshooting](#troubleshooting)

## Overview

CoderamaOpsAI is a full-stack microservices-based system featuring:
- **Frontend:** React + TypeScript + Vite + TailwindCSS
- **Backend:** RESTful API with JWT authentication
- **Background Processing:** MassTransit/RabbitMQ for async jobs
- **Database:** PostgreSQL with Entity Framework Core
- **Testing:** Comprehensive unit and integration tests with Testcontainers
- **Containerization:** Full Docker Compose setup

## Prerequisites

Before starting, ensure you have the following installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (version 8.0 or later)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for PostgreSQL and RabbitMQ)
- [Git](https://git-scm.com/downloads)
- A code editor (Visual Studio 2022, VS Code, or Rider recommended)

### Verify installations:
```bash
dotnet --version  # Should show 8.0.x or higher
docker --version  # Should show Docker version
```

## Quick Start

### Option 1: Docker Compose (Recommended - Easiest)

This is the fastest way to get everything running. Docker Compose will start all services: Frontend, API, Worker, PostgreSQL, and RabbitMQ.

#### 1. Clone the Repository
```bash
git clone <repository-url>
cd CoderamaOpsAI
```

#### 2. Start All Services
```bash
# Start all services with Docker Compose
docker-compose up -d --build

# Verify all services are running
docker-compose ps
```

#### 3. Access the Application
- **Frontend:** http://localhost:3000
- **API:** http://localhost:5000
- **Swagger UI:** http://localhost:5000/swagger
- **RabbitMQ Management:** http://localhost:15672 (guest/guest)

**Note:** Database migrations are applied automatically on API startup.

---

### Option 2: Manual Setup (For Development)

Follow these steps if you want to run services individually:

#### 1. Clone the Repository
```bash
git clone <repository-url>
cd CoderamaOpsAI
```

#### 2. Start PostgreSQL Database
```bash
# Start PostgreSQL using Docker Compose
docker-compose up -d db

# Verify the database is running
docker ps | grep postgres
```

#### 3. Start RabbitMQ (Required for Worker)
```bash
# Start RabbitMQ using Docker Compose
docker-compose up -d rabbitmq

# Verify RabbitMQ is running
docker ps | grep rabbitmq
```

#### 4. Build the Solution
```bash
# Restore dependencies and build all projects
dotnet build CoderamaOpsAI.sln
```

#### 5. Run Database Migrations
```bash
# Apply migrations (creates tables and seeds initial data)
dotnet ef database update --project CoderamaOpsAI.Dal --startup-project CoderamaOpsAI.Api
```

#### 6. Run the API
```bash
# Start the Web API
dotnet run --project CoderamaOpsAI.Api
```

The API will start on `http://localhost:5000`

#### 7. Run the Worker (Optional - in a new terminal)
```bash
# Start the background worker
dotnet run --project CoderamaOpsAI.Worker
```

#### 8. Run the Frontend (in a new terminal)
```bash
cd coderama-ops-frontend

# Install dependencies (first time only)
npm install

# Start the development server
npm run dev
```

The frontend will start on `http://localhost:5173` (or the next available port)

## Project Structure

```
CoderamaOpsAI/
├── coderama-ops-frontend/          # React Frontend (TypeScript, Vite, TailwindCSS)
├── CoderamaOpsAI.Api/              # Web API (Controllers, JWT Auth)
├── CoderamaOpsAI.Dal/              # Data Access Layer (EF Core, Entities)
├── CoderamaOpsAI.Worker/           # Background Jobs & Message Consumers
├── CoderamaOpsAI.Common/           # Shared Infrastructure (MassTransit, Events)
├── CoderamaOpsAI.UnitTests/        # Unit Tests (xUnit)
├── CoderamaOpsAI.IntegrationTests/ # Integration Tests (Testcontainers)
└── docker-compose.yml              # Docker Compose configuration (all services)
```

### Key Components

**coderama-ops-frontend**
- React 19 with TypeScript
- Vite for build tooling and hot module replacement
- TailwindCSS for styling
- React Router for navigation
- Axios for API communication
- JWT-based authentication with session management
- Features: Login, Product browsing, Order management

**CoderamaOpsAI.Api**
- ASP.NET Core Web API with controllers
- Custom JWT authentication (no ASP.NET Identity)
- Swagger/OpenAPI documentation
- Endpoints: Users, Products, Orders (all protected with `[Authorize]`)
- CORS enabled for frontend communication

**CoderamaOpsAI.Dal**
- Entity Framework Core with PostgreSQL
- Code-first migrations
- Domain entities with audit fields (CreatedAt, UpdatedAt)

**CoderamaOpsAI.Worker**
- MassTransit consumers for async message processing
- BackgroundService-based scheduled jobs
- Order expiration monitoring
- Requires RabbitMQ to be running

**CoderamaOpsAI.Common**
- MassTransit/RabbitMQ configuration
- Event bus and integration events
- Shared services between API and Worker

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Unit Tests Only
```bash
dotnet test CoderamaOpsAI.UnitTests
```

### Run Integration Tests Only
```bash
dotnet test CoderamaOpsAI.IntegrationTests
```

### Run a Specific Test
```bash
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

**Note:** Integration tests use Testcontainers and will automatically start PostgreSQL and RabbitMQ containers during test execution.

## API Documentation

### Access Swagger UI
Once the API is running, navigate to:
- **Swagger UI:** http://localhost:5000/swagger

### Authentication Flow

1. **Login to get JWT token:**
   ```bash
   POST http://localhost:5000/api/auth/login
   Content-Type: application/json

   {
     "email": "user@example.com",
     "password": "your-password"
   }
   ```

2. **Use the token in subsequent requests:**
   ```bash
   Authorization: Bearer <your-jwt-token>
   ```

### Key Endpoints
- `POST /api/auth/login` - User login
- `GET /api/users` - List all users
- `GET /api/products` - List all products
- `GET /api/orders` - List all orders
- `POST /api/orders` - Create new order

## Service URLs & Credentials

### Frontend
- **URL:** http://localhost:3000 (Docker) or http://localhost:5173 (dev mode)
- **Test Users:**
  - Admin: admin@coderama.com / Admin123!
  - Test User: testuser@coderama.com / Test123!

### API
- **URL:** http://localhost:5000
- **Swagger:** http://localhost:5000/swagger

### PostgreSQL
- **Host:** localhost
- **Port:** 5432
- **Database:** coderamaopsai
- **Username:** postgres
- **Password:** postgres

### RabbitMQ
- **AMQP Port:** localhost:5672
- **Management UI:** http://localhost:15672
- **Username:** guest
- **Password:** guest

## Troubleshooting

### Frontend Issues
```bash
# Check if frontend is running
docker ps | grep frontend

# View frontend logs
docker-compose logs -f frontend

# Rebuild frontend (after code changes)
docker-compose up -d --build frontend

# Clear browser cache and localStorage if login issues persist
# In browser DevTools: Application > Storage > Clear site data
```

### Database Connection Issues
```bash
# Check if PostgreSQL is running
docker ps | grep postgres

# Restart PostgreSQL
docker-compose restart db

# View PostgreSQL logs
docker-compose logs -f db
```

### RabbitMQ Connection Issues
```bash
# Check if RabbitMQ is running
docker ps | grep rabbitmq

# Restart RabbitMQ
docker-compose restart rabbitmq

# View RabbitMQ logs
docker-compose logs -f rabbitmq
```

### Worker Won't Start
**Error:** "RabbitMQ connection failed"
**Solution:** Ensure RabbitMQ is running before starting the Worker:
```bash
docker ps | grep rabbitmq
```

### Migration Issues
```bash
# Reset database (WARNING: deletes all data)
docker-compose down -v
docker-compose up -d db

# Reapply migrations
dotnet ef database update --project CoderamaOpsAI.Dal --startup-project CoderamaOpsAI.Api
```

### Port Already in Use
If you get "port already in use" errors:
```bash
# Find process using port 5000 (API)
netstat -ano | findstr :5000

# Kill the process (replace PID with actual process ID)
taskkill /PID <PID> /F
```

## Development Commands

### Entity Framework Migrations
```bash
# Create a new migration
dotnet ef migrations add <MigrationName> --project CoderamaOpsAI.Dal --startup-project CoderamaOpsAI.Api

# Apply migrations
dotnet ef database update --project CoderamaOpsAI.Dal --startup-project CoderamaOpsAI.Api

# Rollback to specific migration
dotnet ef database update <MigrationName> --project CoderamaOpsAI.Dal --startup-project CoderamaOpsAI.Api
```

### Docker Commands
```bash
# Start all services (Frontend, API, Worker, PostgreSQL, RabbitMQ)
docker-compose up -d

# Start with rebuild (recommended after code changes)
docker-compose up -d --build

# Stop all services
docker-compose down

# Stop and remove volumes (clears database and message queues)
docker-compose down -v

# View logs for specific service
docker-compose logs -f frontend
docker-compose logs -f api
docker-compose logs -f worker
docker-compose logs -f db
docker-compose logs -f rabbitmq

# Rebuild and restart specific service
docker-compose up -d --build api
docker-compose up -d --build frontend

# Check status of all services
docker-compose ps
```

## Additional Resources

### Backend
- [.NET 8 Documentation](https://docs.microsoft.com/dotnet)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [MassTransit Documentation](https://masstransit-project.com)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

### Frontend
- [React Documentation](https://react.dev)
- [Vite Documentation](https://vitejs.dev)
- [TailwindCSS Documentation](https://tailwindcss.com/docs)
- [TypeScript Documentation](https://www.typescriptlang.org/docs/)

### DevOps
- [Docker Documentation](https://docs.docker.com)
- [Docker Compose Documentation](https://docs.docker.com/compose/)

## Support

For issues or questions, please contact the development team or create an issue in the repository.
