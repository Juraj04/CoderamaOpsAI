# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build entire solution
dotnet build CoderamaOpsAI.sln

# Run the API
dotnet run --project CoderamaOpsAI.Api

# Run the Worker
dotnet run --project CoderamaOpsAI.Worker

# Run all tests
dotnet test

# Run a single test project
dotnet test CoderamaOpsAI.UnitTests
dotnet test CoderamaOpsAI.IntegrationTests

# Run a single test by name
dotnet test --filter "FullyQualifiedName~TestMethodName"

# EF Core migrations (run from solution root, targeting Dal project)
dotnet ef migrations add <MigrationName> --project CoderamaOpsAI.Dal --startup-project CoderamaOpsAI.Api
dotnet ef database update --project CoderamaOpsAI.Dal --startup-project CoderamaOpsAI.Api
```

## Docker Commands

```bash
# Start all services (API + PostgreSQL)
docker-compose up -d

# Start with rebuild
docker-compose up -d --build

# Stop all services
docker-compose down

# Stop and remove volumes (clears database)
docker-compose down -v

# View logs
docker-compose logs -f api
docker-compose logs -f db

# Rebuild API image only
docker-compose build api
```

**Service URLs:**
- API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- PostgreSQL: localhost:5432 (user: postgres, password: postgres, database: coderamaopsai)

**Note:** Migrations are applied automatically on API startup.

## Architecture Overview

This is a modular .NET 8 backend with event-driven processing:

**CoderamaOpsAI.Api** - ASP.NET Core Web API (controllers-based)
- Authentication: Custom JWT-based auth with Users table (no ASP.NET Identity)
- CRUD endpoints for Users, Products, Orders (all protected with `[Authorize]`)
- OpenAPI/Swagger documentation

**CoderamaOpsAI.Dal** - Data Access Layer
- Entity Framework Core with PostgreSQL (code-first)
- Migrations and seeding (via migration `Up` methods)
- Domain entities

**CoderamaOpsAI.Worker** - Background Processing
- MassTransit consumers for async message processing
- BackgroundService-based scheduled jobs (cron-like)

**CoderamaOpsAI.Common** - Shared Infrastructure
- MassTransit/RabbitMQ initialization
- Event bus, integration events/commands
- Cross-cutting concerns shared between Api and Worker

**Tests**
- Unit tests: xUnit + FluentAssertions + NSubstitute
- Integration tests: Testcontainers (PostgreSQL, RabbitMQ) + WebApplicationFactory

## Key Technologies

- .NET 8 (LTS)
- PostgreSQL + EF Core
- RabbitMQ + MassTransit (publish/subscribe messaging)
- JWT authentication
- xUnit + Testcontainers for testing
