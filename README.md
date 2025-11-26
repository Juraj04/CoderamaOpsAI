# CoderamaOpsAI

A modular .NET 8 backend system with event-driven architecture for order processing and management.

## Table of Contents
- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Project Structure](#project-structure)
- [Running Tests](#running-tests)
- [API Documentation](#api-documentation)
- [Troubleshooting](#troubleshooting)

## Overview

CoderamaOpsAI is a microservices-based system featuring:
- RESTful API with JWT authentication
- Background job processing with MassTransit/RabbitMQ
- PostgreSQL database with Entity Framework Core
- Comprehensive testing with Testcontainers

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

Follow these steps to get the application running on your local machine:

### 1. Clone the Repository
```bash
git clone <repository-url>
cd CoderamaOpsAI
```

### 2. Start PostgreSQL Database
```bash
# Start PostgreSQL using Docker Compose
docker-compose up -d db

# Verify the database is running
docker ps | grep postgres
```

### 3. Start RabbitMQ (Required for Worker)
```bash
# Start RabbitMQ with management UI
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Verify RabbitMQ is running
docker ps | grep rabbitmq
```

### 4. Build the Solution
```bash
# Restore dependencies and build all projects
dotnet build CoderamaOpsAI.sln
```

### 5. Run Database Migrations
```bash
# Apply migrations (creates tables and seeds initial data)
dotnet ef database update --project CoderamaOpsAI.Dal --startup-project CoderamaOpsAI.Api
```

### 6. Run the API
```bash
# Start the Web API
dotnet run --project CoderamaOpsAI.Api
```

The API will start on `http://localhost:5000`

### 7. Run the Worker (Optional - in a new terminal)
```bash
# Start the background worker
dotnet run --project CoderamaOpsAI.Worker
```

## Project Structure

```
CoderamaOpsAI/
├── CoderamaOpsAI.Api/              # Web API (Controllers, JWT Auth)
├── CoderamaOpsAI.Dal/              # Data Access Layer (EF Core, Entities)
├── CoderamaOpsAI.Worker/           # Background Jobs & Message Consumers
├── CoderamaOpsAI.Common/           # Shared Infrastructure (MassTransit, Events)
├── CoderamaOpsAI.UnitTests/        # Unit Tests (xUnit)
├── CoderamaOpsAI.IntegrationTests/ # Integration Tests (Testcontainers)
└── docker-compose.yml              # Docker configuration
```

### Key Components

**CoderamaOpsAI.Api**
- ASP.NET Core Web API with controllers
- Custom JWT authentication (no ASP.NET Identity)
- Swagger/OpenAPI documentation
- Endpoints: Users, Products, Orders (all protected with `[Authorize]`)

**CoderamaOpsAI.Dal**
- Entity Framework Core with PostgreSQL
- Code-first migrations
- Domain entities with audit fields (CreatedAt, UpdatedAt)

**CoderamaOpsAI.Worker**
- MassTransit consumers for async message processing
- BackgroundService-based scheduled jobs
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
docker stop rabbitmq
docker rm rabbitmq
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
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
# Start all services (API + Database)
docker-compose up -d

# Start with rebuild
docker-compose up -d --build

# Stop all services
docker-compose down

# Stop and remove volumes (clears database)
docker-compose down -v

# View API logs
docker-compose logs -f api

# View database logs
docker-compose logs -f db
```

## Additional Resources

- [.NET 8 Documentation](https://docs.microsoft.com/dotnet)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [MassTransit Documentation](https://masstransit-project.com)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

## Support

For issues or questions, please contact the development team or create an issue in the repository.
