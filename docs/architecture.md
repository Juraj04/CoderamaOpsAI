<!-- docs/architecture.md -->

# CoderamaOpsAI – Architecture & Technology Overview

## 1. Summary

**CoderamaOpsAI** is a modular .NET 8 backend composed of:

- A **Web API** for authentication and CRUD operations over Users, Products, and Orders.
- A **background worker** for event-driven processing and scheduled (cron-like) jobs.
- A **shared common library** for messaging, events, and cross-cutting infrastructure.
- A **PostgreSQL** database accessed via **EF Core** in a code-first manner.
- **RabbitMQ + MassTransit** for asynchronous, event-driven processing.
- A **test suite** with unit tests (xUnit) and integration tests (Testcontainers).

The project is containerized with Docker and orchestrated using `docker-compose`.

---

## 2. Technologies

### 2.1 Core platform

- **.NET**: .NET 8 (LTS)
- **Web framework**: ASP.NET Core 8 (Controllers-based Web API)
- **Language**: C#

### 2.2 Database & persistence

- **Database**: PostgreSQL
- **ORM**: Entity Framework Core (code-first)
- **Migrations**: EF Core migrations stored in `CoderamaOpsAI.Dal`
- **Seeding**: Via EF Core migrations (`Up` method inserting data)

### 2.3 Authentication & authorization

- **Custom Users table** (no ASP.NET Identity)
- **Auth flow**:
  - Login endpoint validates user credentials against Users table.
  - On success, issues a **JWT**.
- **JWT**:
  - `Microsoft.AspNetCore.Authentication.JwtBearer`
  - Configurable issuer, audience, signing key (e.g. symmetric key in config).
- **Authorization**:
  - All CRUD endpoints protected via `[Authorize]` attribute.
  - `Users`, `Products`, `Orders` controllers require valid JWT.

### 2.4 Messaging & async processing

- **Message broker**: RabbitMQ
- **Messaging library**: MassTransit
- **Patterns**:
  - Publish/subscribe via integration events.
  - Consumers in `CoderamaOpsAI.Worker` process messages asynchronously.

### 2.5 Background jobs (cron-like)

- **Mechanism**: `BackgroundService`-based hosted services
- **Scheduling**: Time-based loops (e.g. `Task.Delay`, `PeriodicTimer`) running every N seconds/minutes.
- **Deployment**: Background services live in `CoderamaOpsAI.Worker`.

### 2.6 Testing

- **Unit tests**:
  - Framework: xUnit
  - Assertions: FluentAssertions
  - Mocking: NSubstitute
- **Integration tests**:
  - Testcontainers for:
    - PostgreSQL
    - RabbitMQ (optionally, or use MassTransit in-memory transport for some tests)
  - ASP.NET Core `WebApplicationFactory`-based tests for the API.

---

## 3. Solution Structure

At the root:

```text
CoderamaOpsAI.sln
/CoderamaOpsAI.Api
/CoderamaOpsAI.Dal
/CoderamaOpsAI.Worker
/CoderamaOpsAI.Common
/tests
  /CoderamaOpsAI.UnitTests
  /CoderamaOpsAI.IntegrationTests
/docs
  architecture.md
  running.md
/docker
  Dockerfile.api
  Dockerfile.worker
  docker-compose.yml
