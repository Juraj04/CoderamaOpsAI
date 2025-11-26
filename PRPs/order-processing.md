# PRP: Event-Driven Order Processing with RabbitMQ & MassTransit

## Confidence Score: 9/10
This PRP provides comprehensive context for one-pass implementation with exact specifications, validation gates, and patterns discovered from codebase exploration. Well-researched with complete MassTransit configuration, consumer patterns, background jobs, and testing strategies.

---

## Overview

Implement asynchronous order processing using event-driven architecture with:
- **OrderCreated** event → Payment processing (50% success rate, simulated)
- **OrderCompleted** event → Email notification + Database audit trail
- **OrderExpired** event → Database audit trail (background job finds stale orders)
- **Background job** running every 60 seconds to expire Processing orders older than 10 minutes

All components use MassTransit + RabbitMQ for messaging, with status-based idempotency, 3-retry policy, and comprehensive unit tests.

---

## Technology Context

- **.NET 8** with PostgreSQL + EF Core
- **Messaging**: RabbitMQ + MassTransit 8.2.0 (publish/subscribe pattern)
- **Architecture**: Event-driven with simple consumers (no saga orchestration)
- **Worker**: BackgroundService for scheduled expiration job
- **Idempotency**: Status-based (check order status before processing)
- **Testing**: xUnit + FluentAssertions + NSubstitute + In-Memory Database
- **Docker**: RabbitMQ with management UI, Worker service added

---

## Implementation Order

### Phase 1: Common Project Foundation (Events & Services)
1. Add MassTransit NuGet packages to Common
2. Create event records (OrderCreatedEvent, OrderCompletedEvent, OrderExpiredEvent)
3. Create IPaymentSimulator interface and implementation (testable 50% logic)
4. Create IEventBus interface and EventBus implementation
5. Create MassTransit configuration (RabbitMqSettings, MassTransitConfiguration)

### Phase 2: Database Layer (Notification Entity)
6. Create Notification entity with NotificationType enum
7. Update AppDbContext with Notifications DbSet and Fluent API configuration
8. Generate and apply EF Core migration

### Phase 3: API Integration (Event Publishing)
9. Add project reference from Api to Common
10. Update OrdersController to inject IEventBus and publish OrderCreated event
11. Update Api Program.cs to register MassTransit (publisher-only)
12. Add RabbitMq configuration to Api appsettings.json

### Phase 4: Worker Consumers (Event Handlers)
13. Add project references and packages to Worker
14. Create OrderCreatedConsumer (payment processing logic)
15. Create OrderCompletedConsumer (notification logic)
16. Create OrderExpiredConsumer (expiration notification logic)

### Phase 5: Background Job (Expiration Processing)
17. Create OrderExpirationJob BackgroundService

### Phase 6: Worker Setup (Configuration)
18. Change Worker project SDK from Web to Worker
19. Update Worker Program.cs with Host builder and service registration
20. Create Worker appsettings.json with DB, RabbitMQ, and job configuration

### Phase 7: Docker Setup (Infrastructure)
21. Create Worker Dockerfile
22. Update docker-compose.yml with RabbitMQ and Worker services

### Phase 8: Testing Layer
23. Add MassTransit.TestFramework to UnitTests
24. Create OrderCreatedConsumerTests (payment success/failure, idempotency)
25. Create OrderCompletedConsumerTests (notification creation, idempotency)
26. Create OrderExpiredConsumerTests (expiration notification)

### Phase 9: Validation
27. Build entire solution
28. Run all unit tests
29. Start Docker services and verify all healthy
30. End-to-end test: create order, verify processing, check notifications

---

## Architecture Overview

```
┌────────────────────────────────────────────────────────────┐
│                  Order Processing Flow                      │
└────────────────────────────────────────────────────────────┘

API (CoderamaOpsAI.Api)
  │
  ├─► POST /api/orders
  │    ├─ Save Order (status: Pending)
  │    └─ Publish OrderCreated Event ──────────────┐
  │                                                  │
  │                                                  ▼
Worker (CoderamaOpsAI.Worker)                  RabbitMQ
  │                                                  │
  ├─► OrderCreatedConsumer ◄────────────────────────┘
  │    ├─ Update: Pending → Processing
  │    ├─ Simulate payment (5s delay)
  │    └─ 50% chance: Completed + Publish OrderCompleted
  │                                                  │
  ├─► OrderCompletedConsumer ◄─────────────────────┤
  │    ├─ Log fake email to console
  │    └─ Save Notification to DB
  │
  ├─► OrderExpiredConsumer ◄───────────────────────┤
  │    └─ Save Notification to DB                  │
  │                                                  │
  └─► OrderExpirationJob (BackgroundService)       │
       ├─ Runs every 60s (configurable)            │
       ├─ Find: status=Processing & age>10min      │
       ├─ Update → Expired                         │
       └─ Publish OrderExpired Event ──────────────┘

Common (CoderamaOpsAI.Common)
  ├─ Event Definitions
  ├─ IEventBus Interface
  ├─ MassTransit Configuration
  └─ IPaymentSimulator (testable randomness)
```

---

## Project Structure & Files

### CoderamaOpsAI.Common/ (8 new files)

**Events/** (3 files)
```
OrderCreatedEvent.cs
OrderCompletedEvent.cs
OrderExpiredEvent.cs
```

**Interfaces/** (2 files)
```
IEventBus.cs
IPaymentSimulator.cs
```

**Services/** (2 files)
```
EventBus.cs
PaymentSimulator.cs
```

**Configuration/** (2 files)
```
MassTransitConfiguration.cs
RabbitMqSettings.cs
```

### CoderamaOpsAI.Worker/ (5 new files + 3 modified)

**Consumers/** (3 files)
```
OrderCreatedConsumer.cs
OrderCompletedConsumer.cs
OrderExpiredConsumer.cs
```

**BackgroundJobs/** (1 file)
```
OrderExpirationJob.cs
```

**Config Files** (2 files)
```
appsettings.json (create)
Dockerfile (create)
```

**Modified Files** (1 file)
```
Program.cs (complete rewrite)
CoderamaOpsAI.Worker.csproj (change SDK)
```

### CoderamaOpsAI.Api/ (2 modified files)
```
Controllers/OrdersController.cs (add event publishing)
Program.cs (register MassTransit)
appsettings.json (add RabbitMq config)
```

### CoderamaOpsAI.Dal/ (1 new file + 1 modified + 1 migration)
```
Entities/Notification.cs (create)
AppDbContext.cs (add Notifications DbSet)
Migrations/[timestamp]_AddNotificationsTable.cs (generate)
```

### CoderamaOpsAI.UnitTests/ (3 new files)
```
Worker/Consumers/OrderCreatedConsumerTests.cs
Worker/Consumers/OrderCompletedConsumerTests.cs
Worker/Consumers/OrderExpiredConsumerTests.cs
```

### Docker (2 modified files)
```
docker-compose.yml (add RabbitMQ + Worker services)
```

---

## NuGet Packages Required

### CoderamaOpsAI.Common
```bash
dotnet add CoderamaOpsAI.Common package MassTransit --version 8.2.0
dotnet add CoderamaOpsAI.Common package MassTransit.RabbitMQ --version 8.2.0
dotnet add CoderamaOpsAI.Common package Microsoft.Extensions.Options --version 8.0.0
```

### CoderamaOpsAI.Api
```bash
dotnet add CoderamaOpsAI.Api reference CoderamaOpsAI.Common
# MassTransit comes transitively from Common
```

### CoderamaOpsAI.Worker
```bash
dotnet add CoderamaOpsAI.Worker package MassTransit --version 8.2.0
dotnet add CoderamaOpsAI.Worker package MassTransit.RabbitMQ --version 8.2.0
dotnet add CoderamaOpsAI.Worker package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.11
dotnet add CoderamaOpsAI.Worker package Microsoft.Extensions.Hosting --version 8.0.0
dotnet add CoderamaOpsAI.Worker reference CoderamaOpsAI.Dal
dotnet add CoderamaOpsAI.Worker reference CoderamaOpsAI.Common
```

### CoderamaOpsAI.UnitTests
```bash
dotnet add CoderamaOpsAI.UnitTests package MassTransit.TestFramework --version 8.2.0
```

---

## Phase 1: Common Project - Events & Services

### Event Definitions

**File**: `CoderamaOpsAI.Common/Events/OrderCreatedEvent.cs`
```csharp
namespace CoderamaOpsAI.Common.Events;

public record OrderCreatedEvent(
    int OrderId,
    int UserId,
    int ProductId,
    decimal Total
);
```

**File**: `CoderamaOpsAI.Common/Events/OrderCompletedEvent.cs`
```csharp
namespace CoderamaOpsAI.Common.Events;

public record OrderCompletedEvent(
    int OrderId,
    int UserId,
    decimal Total
);
```

**File**: `CoderamaOpsAI.Common/Events/OrderExpiredEvent.cs`
```csharp
namespace CoderamaOpsAI.Common.Events;

public record OrderExpiredEvent(
    int OrderId,
    int UserId
);
```

### Payment Simulator (Testable Randomness)

**File**: `CoderamaOpsAI.Common/Interfaces/IPaymentSimulator.cs`
```csharp
namespace CoderamaOpsAI.Common.Interfaces;

public interface IPaymentSimulator
{
    bool ShouldCompletePayment();
}
```

**File**: `CoderamaOpsAI.Common/Services/PaymentSimulator.cs`
```csharp
namespace CoderamaOpsAI.Common.Services;

public class PaymentSimulator : IPaymentSimulator
{
    private readonly Random _random = new();

    public bool ShouldCompletePayment()
    {
        return _random.Next(0, 2) == 1; // 50% chance
    }
}
```

**Why this pattern?**: In production, randomness is fine. In tests, we mock `IPaymentSimulator` to return deterministic results (true/false), making tests reliable and predictable.

### Event Bus Abstraction

**File**: `CoderamaOpsAI.Common/Interfaces/IEventBus.cs`
```csharp
namespace CoderamaOpsAI.Common.Interfaces;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken) where T : class;
}
```

**File**: `CoderamaOpsAI.Common/Services/EventBus.cs`
```csharp
using MassTransit;

namespace CoderamaOpsAI.Common.Services;

public class EventBus : IEventBus
{
    private readonly IPublishEndpoint _publishEndpoint;

    public EventBus(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken) where T : class
    {
        await _publishEndpoint.Publish(@event, cancellationToken);
    }
}
```

**Why wrap IPublishEndpoint?**: Abstraction allows easier testing (mock IEventBus instead of MassTransit internals) and provides a consistent API across the application.

### MassTransit Configuration

**File**: `CoderamaOpsAI.Common/Configuration/RabbitMqSettings.cs`
```csharp
namespace CoderamaOpsAI.Common.Configuration;

public class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}
```

**File**: `CoderamaOpsAI.Common/Configuration/MassTransitConfiguration.cs`
```csharp
using CoderamaOpsAI.Common.Interfaces;
using CoderamaOpsAI.Common.Services;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoderamaOpsAI.Common.Configuration;

public static class MassTransitConfiguration
{
    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        var rabbitMqSettings = configuration.GetSection("RabbitMq").Get<RabbitMqSettings>()
            ?? throw new InvalidOperationException("RabbitMq configuration missing");

        services.AddMassTransit(x =>
        {
            // Register consumers if provided (Worker only)
            configureConsumers?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqSettings.Host, rabbitMqSettings.VirtualHost, h =>
                {
                    h.Username(rabbitMqSettings.Username);
                    h.Password(rabbitMqSettings.Password);
                });

                // Retry policy: 3 attempts, exponential backoff
                cfg.UseMessageRetry(r => r.Exponential(
                    retryLimit: 3,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(2)
                ));

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IEventBus, EventBus>();
        services.AddSingleton<IPaymentSimulator, PaymentSimulator>();

        return services;
    }
}
```

**Key patterns**:
- **Optional consumer registration**: API publishes only (no consumers), Worker consumes (registers consumers)
- **Retry policy**: 3 attempts with exponential backoff (1s → 3s → 7s)
- **ConfigureEndpoints**: Auto-creates queues based on consumer types

---

## Phase 2: Database Layer - Notification Entity

### Notification Entity

**File**: `CoderamaOpsAI.Dal/Entities/Notification.cs`
```csharp
namespace CoderamaOpsAI.Dal.Entities;

public class Notification
{
    public int Id { get; set; }
    public int? OrderId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Metadata { get; set; }  // JSON for userId, total, etc.
    public DateTime CreatedAt { get; set; }

    public Order? Order { get; set; }
}

public enum NotificationType
{
    OrderCompleted,
    OrderExpired
}
```

**Design decisions**:
- **OrderId nullable**: Allows system-wide notifications not tied to specific orders
- **Metadata as JSON**: Flexible storage for order details, user info, timestamps
- **Type enum**: Strongly typed notification categories

### AppDbContext Update

**File**: `CoderamaOpsAI.Dal/AppDbContext.cs`

**Add DbSet** (after line 14):
```csharp
public DbSet<Notification> Notifications => Set<Notification>();
```

**Add entity configuration** (in OnModelCreating, after Order configuration):
```csharp
modelBuilder.Entity<Notification>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).ValueGeneratedOnAdd();
    entity.Property(e => e.Type).IsRequired()
        .HasConversion<string>()  // Store enum as string
        .HasMaxLength(50);
    entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
    entity.Property(e => e.Metadata).HasMaxLength(2000);
    entity.Property(e => e.CreatedAt).IsRequired();

    entity.HasOne(e => e.Order)
        .WithMany()
        .HasForeignKey(e => e.OrderId)
        .OnDelete(DeleteBehavior.SetNull);  // Keep notification if order deleted
});
```

### Migration

**Create migration**:
```bash
dotnet ef migrations add AddNotificationsTable --project CoderamaOpsAI.Dal --startup-project CoderamaOpsAI.Api
```

**Apply migration** (automatically via Docker startup, or manually):
```bash
dotnet ef database update --project CoderamaOpsAI.Dal --startup-project CoderamaOpsAI.Api
```

---

## Phase 3: API Integration - Event Publishing

### OrdersController Update

**File**: `CoderamaOpsAI.Api/Controllers/OrdersController.cs`

**Add IEventBus injection** (modify constructor at line 16-22):
```csharp
private readonly AppDbContext _dbContext;
private readonly ILogger<OrdersController> _logger;
private readonly IEventBus _eventBus;  // NEW

public OrdersController(AppDbContext dbContext, ILogger<OrdersController> logger, IEventBus eventBus)
{
    _dbContext = dbContext;
    _logger = logger;
    _eventBus = eventBus;  // NEW
}
```

**Add event publishing** (in Create method, after line 121):
```csharp
await _dbContext.SaveChangesAsync(cancellationToken);

// Publish OrderCreated event
await _eventBus.PublishAsync(new OrderCreatedEvent(
    order.Id,
    order.UserId,
    order.ProductId,
    order.Total
), cancellationToken);

_logger.LogInformation("Order {OrderId} created and event published", order.Id);

// Load navigation properties for response...
```

**Add using statement** (top of file):
```csharp
using CoderamaOpsAI.Common.Events;
using CoderamaOpsAI.Common.Interfaces;
```

### Api Program.cs Update

**File**: `CoderamaOpsAI.Api/Program.cs`

**Add MassTransit registration** (after DbContext registration, line 55):
```csharp
// Add MassTransit for event publishing (no consumers in API)
builder.Services.AddEventBus(builder.Configuration);
```

**Add using statement** (top of file):
```csharp
using CoderamaOpsAI.Common.Configuration;
```

### Api appsettings.json Update

**File**: `CoderamaOpsAI.Api/appsettings.json`

**Add RabbitMq configuration** (after ConnectionStrings):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "RabbitMq": {
    "Host": "localhost",
    "VirtualHost": "/",
    "Username": "guest",
    "Password": "guest"
  },
  "Jwt": {
    ...
  }
}
```

**Note**: In Docker, `Host` will be `"rabbitmq"` (service name), configured via environment variables in docker-compose.yml.

---

## Phase 4: Worker Consumers - Event Handlers

### OrderCreatedConsumer (Payment Processing)

**File**: `CoderamaOpsAI.Worker/Consumers/OrderCreatedConsumer.cs`
```csharp
using CoderamaOpsAI.Common.Events;
using CoderamaOpsAI.Common.Interfaces;
using CoderamaOpsAI.Dal;
using CoderamaOpsAI.Dal.Entities;
using MassTransit;

namespace CoderamaOpsAI.Worker.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly AppDbContext _dbContext;
    private readonly IPaymentSimulator _paymentSimulator;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(
        AppDbContext dbContext,
        IPaymentSimulator paymentSimulator,
        IEventBus eventBus,
        ILogger<OrderCreatedConsumer> logger)
    {
        _dbContext = dbContext;
        _paymentSimulator = paymentSimulator;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var orderId = context.Message.OrderId;
        _logger.LogInformation("Processing OrderCreated for OrderId={OrderId}", orderId);

        var order = await _dbContext.Orders.FindAsync(orderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found, skipping", orderId);
            return;
        }

        // Idempotency: skip if already processed
        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogInformation("Order {OrderId} already processed (status={Status})",
                orderId, order.Status);
            return;
        }

        // Update to Processing
        order.Status = OrderStatus.Processing;
        order.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Order {OrderId} status updated to Processing", orderId);

        // Simulate payment (5 seconds)
        await Task.Delay(TimeSpan.FromSeconds(5), context.CancellationToken);

        // 50% chance to complete
        if (_paymentSimulator.ShouldCompletePayment())
        {
            order.Status = OrderStatus.Completed;
            order.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            await _eventBus.PublishAsync(new OrderCompletedEvent(
                order.Id,
                order.UserId,
                order.Total
            ), context.CancellationToken);

            _logger.LogInformation("Order {OrderId} completed and event published", orderId);
        }
        else
        {
            _logger.LogInformation("Order {OrderId} remains in Processing (payment pending)", orderId);
        }
    }
}
```

**Key patterns**:
- **Idempotency**: Check `order.Status != OrderStatus.Pending` to prevent reprocessing
- **Two SaveChanges**: One for Processing, one for Completed (separate states)
- **CancellationToken**: Passed to Task.Delay for graceful shutdown support

### OrderCompletedConsumer (Notification)

**File**: `CoderamaOpsAI.Worker/Consumers/OrderCompletedConsumer.cs`
```csharp
using System.Text.Json;
using CoderamaOpsAI.Common.Events;
using CoderamaOpsAI.Dal;
using CoderamaOpsAI.Dal.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CoderamaOpsAI.Worker.Consumers;

public class OrderCompletedConsumer : IConsumer<OrderCompletedEvent>
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OrderCompletedConsumer> _logger;

    public OrderCompletedConsumer(AppDbContext dbContext, ILogger<OrderCompletedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Order {OrderId} completed for User {UserId}, Total={Total}",
            msg.OrderId, msg.UserId, msg.Total);

        // Fake email notification (log to console)
        _logger.LogInformation("📧 FAKE EMAIL: Order #{OrderId} completed! Total: ${Total}",
            msg.OrderId, msg.Total);

        // Idempotency: check if notification already exists
        var exists = await _dbContext.Notifications.AnyAsync(n =>
            n.OrderId == msg.OrderId &&
            n.Type == NotificationType.OrderCompleted);

        if (!exists)
        {
            var notification = new Notification
            {
                OrderId = msg.OrderId,
                Type = NotificationType.OrderCompleted,
                Message = $"Order #{msg.OrderId} completed successfully",
                Metadata = JsonSerializer.Serialize(new { msg.UserId, msg.Total }),
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Notifications.AddAsync(notification);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Notification saved for OrderId={OrderId}", msg.OrderId);
        }
        else
        {
            _logger.LogInformation("Notification already exists for OrderId={OrderId}", msg.OrderId);
        }
    }
}
```

**Key patterns**:
- **Fake email**: Log to console with emoji for visibility in logs
- **Idempotency**: Use `AnyAsync` to check if notification already created
- **Metadata as JSON**: Store additional context for debugging

### OrderExpiredConsumer (Expiration Notification)

**File**: `CoderamaOpsAI.Worker/Consumers/OrderExpiredConsumer.cs`
```csharp
using System.Text.Json;
using CoderamaOpsAI.Common.Events;
using CoderamaOpsAI.Dal;
using CoderamaOpsAI.Dal.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CoderamaOpsAI.Worker.Consumers;

public class OrderExpiredConsumer : IConsumer<OrderExpiredEvent>
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OrderExpiredConsumer> _logger;

    public OrderExpiredConsumer(AppDbContext dbContext, ILogger<OrderExpiredConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderExpiredEvent> context)
    {
        var msg = context.Message;
        _logger.LogWarning("Order {OrderId} expired for User {UserId}", msg.OrderId, msg.UserId);

        // Idempotency: check if notification already exists
        var exists = await _dbContext.Notifications.AnyAsync(n =>
            n.OrderId == msg.OrderId &&
            n.Type == NotificationType.OrderExpired);

        if (!exists)
        {
            var notification = new Notification
            {
                OrderId = msg.OrderId,
                Type = NotificationType.OrderExpired,
                Message = $"Order #{msg.OrderId} expired after 10 minutes in Processing status",
                Metadata = JsonSerializer.Serialize(new { msg.UserId, ExpiredAt = DateTime.UtcNow }),
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Notifications.AddAsync(notification);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Expiration notification saved for OrderId={OrderId}", msg.OrderId);
        }
    }
}
```

---

## Phase 5: Background Job - Order Expiration

### OrderExpirationJob

**File**: `CoderamaOpsAI.Worker/BackgroundJobs/OrderExpirationJob.cs`
```csharp
using CoderamaOpsAI.Common.Events;
using CoderamaOpsAI.Common.Interfaces;
using CoderamaOpsAI.Dal;
using CoderamaOpsAI.Dal.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoderamaOpsAI.Worker.BackgroundJobs;

public class OrderExpirationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderExpirationJob> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _expirationThreshold;

    public OrderExpirationJob(
        IServiceProvider serviceProvider,
        ILogger<OrderExpirationJob> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(
            configuration.GetValue<int>("OrderExpiration:IntervalSeconds", 60));
        _expirationThreshold = TimeSpan.FromMinutes(10);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderExpirationJob started (interval={Interval}s)",
            _interval.TotalSeconds);

        // Wait a bit before first execution to let services initialize
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredOrders(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OrderExpirationJob");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessExpiredOrders(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var cutoffTime = DateTime.UtcNow - _expirationThreshold;

        var expiredOrders = await dbContext.Orders
            .Where(o => o.Status == OrderStatus.Processing && o.UpdatedAt < cutoffTime)
            .ToListAsync(cancellationToken);

        if (expiredOrders.Count == 0)
        {
            _logger.LogDebug("No expired orders found (cutoff: {Cutoff})", cutoffTime);
            return;
        }

        _logger.LogInformation("Found {Count} expired orders", expiredOrders.Count);

        foreach (var order in expiredOrders)
        {
            order.Status = OrderStatus.Expired;
            order.UpdatedAt = DateTime.UtcNow;

            await eventBus.PublishAsync(new OrderExpiredEvent(order.Id, order.UserId),
                cancellationToken);

            _logger.LogInformation("Order {OrderId} marked as expired and event published", order.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

**Key patterns**:
- **IServiceProvider injection**: BackgroundService is singleton, but DbContext is scoped (create scope per iteration)
- **Configurable interval**: Read from appsettings, default 60 seconds
- **Startup delay**: Wait 5 seconds before first run to let services initialize
- **UpdatedAt for expiration**: Compare `UpdatedAt` (not CreatedAt) to detect stale processing orders

---

## Phase 6: Worker Setup - Configuration

### Worker Project File Update

**File**: `CoderamaOpsAI.Worker/CoderamaOpsAI.Worker.csproj`

**Change SDK** (line 1):
```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">  <!-- Changed from Sdk.Web -->
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MassTransit" Version="8.2.0" />
    <PackageReference Include="MassTransit.RabbitMQ" Version="8.2.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.11" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\CoderamaOpsAI.Dal\CoderamaOpsAI.Dal.csproj" />
    <ProjectReference Include="..\CoderamaOpsAI.Common\CoderamaOpsAI.Common.csproj" />
  </ItemGroup>
</Project>
```

### Worker Program.cs Update

**File**: `CoderamaOpsAI.Worker/Program.cs`

**Replace entire file**:
```csharp
using CoderamaOpsAI.Common.Configuration;
using CoderamaOpsAI.Dal;
using CoderamaOpsAI.Worker.BackgroundJobs;
using CoderamaOpsAI.Worker.Consumers;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Add DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));

        // Add MassTransit with consumers
        services.AddEventBus(context.Configuration, x =>
        {
            x.AddConsumer<OrderCreatedConsumer>();
            x.AddConsumer<OrderCompletedConsumer>();
            x.AddConsumer<OrderExpiredConsumer>();
        });

        // Add background job
        services.AddHostedService<OrderExpirationJob>();
    });

await builder.Build().RunAsync();
```

**Key differences from API**:
- Uses `Host.CreateDefaultBuilder` instead of `WebApplication.CreateBuilder`
- Registers consumers in `AddEventBus` (API doesn't register consumers)
- Registers `OrderExpirationJob` as hosted service

### Worker appsettings.json

**File**: `CoderamaOpsAI.Worker/appsettings.json` (create new file)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning",
      "MassTransit": "Information",
      "CoderamaOpsAI.Worker": "Debug"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=coderamaopsai;Username=postgres;Password=postgres"
  },
  "RabbitMq": {
    "Host": "localhost",
    "VirtualHost": "/",
    "Username": "guest",
    "Password": "guest"
  },
  "OrderExpiration": {
    "IntervalSeconds": 60
  }
}
```

**Note**: In Docker, connection strings and RabbitMq host are overridden via environment variables in docker-compose.yml.

---

## Phase 7: Docker Setup - Infrastructure

### Worker Dockerfile

**File**: `CoderamaOpsAI.Worker/Dockerfile` (create new file)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CoderamaOpsAI.Worker/CoderamaOpsAI.Worker.csproj", "CoderamaOpsAI.Worker/"]
COPY ["CoderamaOpsAI.Dal/CoderamaOpsAI.Dal.csproj", "CoderamaOpsAI.Dal/"]
COPY ["CoderamaOpsAI.Common/CoderamaOpsAI.Common.csproj", "CoderamaOpsAI.Common/"]
RUN dotnet restore "CoderamaOpsAI.Worker/CoderamaOpsAI.Worker.csproj"
COPY . .
WORKDIR "/src/CoderamaOpsAI.Worker"
RUN dotnet build "CoderamaOpsAI.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CoderamaOpsAI.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoderamaOpsAI.Worker.dll"]
```

**Pattern**: Multi-stage build for smaller final image (same as Api Dockerfile pattern).

### docker-compose.yml Update

**File**: `docker-compose.yml`

**Replace entire file**:
```yaml
services:
  api:
    build:
      context: .
      dockerfile: CoderamaOpsAI.Api/Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=coderamaopsai;Username=postgres;Password=postgres
      - Jwt__Key=${JWT_KEY}
      - Jwt__Issuer=${JWT_ISSUER}
      - Jwt__Audience=${JWT_AUDIENCE}
      - RabbitMq__Host=rabbitmq
      - RabbitMq__Username=guest
      - RabbitMq__Password=guest
    depends_on:
      db:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
    restart: unless-stopped

  worker:
    build:
      context: .
      dockerfile: CoderamaOpsAI.Worker/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=coderamaopsai;Username=postgres;Password=postgres
      - RabbitMq__Host=rabbitmq
      - RabbitMq__Username=guest
      - RabbitMq__Password=guest
      - OrderExpiration__IntervalSeconds=60
    depends_on:
      db:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
    restart: unless-stopped

  db:
    image: postgres:16-alpine
    ports:
      - "5432:5432"
    environment:
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=postgres
      - POSTGRES_DB=coderamaopsai
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5
    restart: unless-stopped

  rabbitmq:
    image: rabbitmq:3.13-management-alpine
    ports:
      - "5672:5672"   # AMQP port
      - "15672:15672" # Management UI
    environment:
      - RABBITMQ_DEFAULT_USER=guest
      - RABBITMQ_DEFAULT_PASS=guest
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped

volumes:
  postgres_data:
  rabbitmq_data:
```

**Key changes**:
- Added `rabbitmq` service with management UI
- Added `worker` service
- Both `api` and `worker` depend on `rabbitmq` health check
- RabbitMQ host overridden to `"rabbitmq"` (service name) via env vars

---

## Phase 8: Testing Layer - Unit Tests

### OrderCreatedConsumerTests

**File**: `CoderamaOpsAI.UnitTests/Worker/Consumers/OrderCreatedConsumerTests.cs`
```csharp
using CoderamaOpsAI.Common.Events;
using CoderamaOpsAI.Common.Interfaces;
using CoderamaOpsAI.Dal.Entities;
using CoderamaOpsAI.UnitTests.Common;
using CoderamaOpsAI.Worker.Consumers;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CoderamaOpsAI.UnitTests.Worker.Consumers;

public class OrderCreatedConsumerTests : DatabaseTestBase
{
    private readonly IPaymentSimulator _paymentSimulator;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly OrderCreatedConsumer _consumer;

    public OrderCreatedConsumerTests()
    {
        _paymentSimulator = Substitute.For<IPaymentSimulator>();
        _eventBus = Substitute.For<IEventBus>();
        _logger = Substitute.For<ILogger<OrderCreatedConsumer>>();
        _consumer = new OrderCreatedConsumer(DbContext, _paymentSimulator, _eventBus, _logger);
    }

    [Fact]
    public async Task Given_PendingOrder_When_PaymentSucceeds_Then_CompletesOrderAndPublishesEvent()
    {
        // Arrange
        var order = new Order
        {
            Id = 1,
            UserId = 1,
            ProductId = 1,
            Quantity = 1,
            Price = 100m,
            Total = 100m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Orders.AddAsync(order);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        _paymentSimulator.ShouldCompletePayment().Returns(true);

        var @event = new OrderCreatedEvent(1, 1, 1, 100m);
        var context = Substitute.For<ConsumeContext<OrderCreatedEvent>>();
        context.Message.Returns(@event);
        context.CancellationToken.Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(context);

        // Assert
        var updatedOrder = await DbContext.Orders.FindAsync(1);
        updatedOrder.Should().NotBeNull();
        updatedOrder!.Status.Should().Be(OrderStatus.Completed);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<OrderCompletedEvent>(e => e.OrderId == 1),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Given_PendingOrder_When_PaymentFails_Then_RemainsProcessing()
    {
        // Arrange
        var order = new Order
        {
            Id = 2,
            UserId = 1,
            ProductId = 1,
            Quantity = 1,
            Price = 50m,
            Total = 50m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Orders.AddAsync(order);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        _paymentSimulator.ShouldCompletePayment().Returns(false);

        var @event = new OrderCreatedEvent(2, 1, 1, 50m);
        var context = Substitute.For<ConsumeContext<OrderCreatedEvent>>();
        context.Message.Returns(@event);
        context.CancellationToken.Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(context);

        // Assert
        var updatedOrder = await DbContext.Orders.FindAsync(2);
        updatedOrder!.Status.Should().Be(OrderStatus.Processing);

        await _eventBus.DidNotReceive().PublishAsync(
            Arg.Any<OrderCompletedEvent>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Given_AlreadyProcessedOrder_When_ConsumeAgain_Then_Idempotent()
    {
        // Arrange - order already in Processing
        var order = new Order
        {
            Id = 3,
            UserId = 1,
            ProductId = 1,
            Quantity = 1,
            Price = 75m,
            Total = 75m,
            Status = OrderStatus.Processing,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Orders.AddAsync(order);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        var @event = new OrderCreatedEvent(3, 1, 1, 75m);
        var context = Substitute.For<ConsumeContext<OrderCreatedEvent>>();
        context.Message.Returns(@event);
        context.CancellationToken.Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(context);

        // Assert - should skip processing
        _paymentSimulator.DidNotReceive().ShouldCompletePayment();
        await _eventBus.DidNotReceive().PublishAsync(
            Arg.Any<OrderCompletedEvent>(),
            Arg.Any<CancellationToken>()
        );
    }
}
```

**Test coverage**:
- Payment success → Order completed, event published
- Payment failure → Order remains Processing, no event
- Idempotency → Already processed orders skipped

### OrderCompletedConsumerTests

**File**: `CoderamaOpsAI.UnitTests/Worker/Consumers/OrderCompletedConsumerTests.cs`
```csharp
using CoderamaOpsAI.Common.Events;
using CoderamaOpsAI.Dal.Entities;
using CoderamaOpsAI.UnitTests.Common;
using CoderamaOpsAI.Worker.Consumers;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CoderamaOpsAI.UnitTests.Worker.Consumers;

public class OrderCompletedConsumerTests : DatabaseTestBase
{
    private readonly ILogger<OrderCompletedConsumer> _logger;
    private readonly OrderCompletedConsumer _consumer;

    public OrderCompletedConsumerTests()
    {
        _logger = Substitute.For<ILogger<OrderCompletedConsumer>>();
        _consumer = new OrderCompletedConsumer(DbContext, _logger);
    }

    [Fact]
    public async Task Given_OrderCompletedEvent_When_Consume_Then_CreatesNotification()
    {
        // Arrange
        var @event = new OrderCompletedEvent(1, 10, 99.99m);
        var context = Substitute.For<ConsumeContext<OrderCompletedEvent>>();
        context.Message.Returns(@event);

        // Act
        await _consumer.Consume(context);

        // Assert
        var notification = await DbContext.Notifications
            .FirstOrDefaultAsync(n => n.OrderId == 1 && n.Type == NotificationType.OrderCompleted);

        notification.Should().NotBeNull();
        notification!.Message.Should().Contain("Order #1 completed");
        notification.Metadata.Should().Contain("UserId");
    }

    [Fact]
    public async Task Given_DuplicateEvent_When_Consume_Then_Idempotent()
    {
        // Arrange - notification already exists
        var existing = new Notification
        {
            OrderId = 2,
            Type = NotificationType.OrderCompleted,
            Message = "Already exists",
            CreatedAt = DateTime.UtcNow
        };
        await DbContext.Notifications.AddAsync(existing);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        var @event = new OrderCompletedEvent(2, 10, 50m);
        var context = Substitute.For<ConsumeContext<OrderCompletedEvent>>();
        context.Message.Returns(@event);

        // Act
        await _consumer.Consume(context);

        // Assert - should not create duplicate
        var count = await DbContext.Notifications
            .CountAsync(n => n.OrderId == 2 && n.Type == NotificationType.OrderCompleted);
        count.Should().Be(1);
    }
}
```

**Test coverage**:
- Event → Notification created with correct data
- Duplicate event → Idempotent (no duplicate notification)

### OrderExpiredConsumerTests

**File**: `CoderamaOpsAI.UnitTests/Worker/Consumers/OrderExpiredConsumerTests.cs`

**Pattern**: Similar to OrderCompletedConsumerTests, but with NotificationType.OrderExpired.

**Test cases**:
- OrderExpired event → Creates expiration notification
- Duplicate event → Idempotent

---

## Critical Patterns & Gotchas

### 1. Idempotency Pattern
**Status-based approach**:
```csharp
// Before processing
if (order.Status != OrderStatus.Pending)
{
    _logger.LogInformation("Already processed, skipping");
    return;
}
```

**Notification deduplication**:
```csharp
var exists = await _dbContext.Notifications.AnyAsync(n =>
    n.OrderId == msg.OrderId &&
    n.Type == NotificationType.OrderCompleted);

if (!exists)
{
    // Create notification
}
```

**Why status-based?**: Simple, sufficient for this use case where state transitions are clear (Pending → Processing → Completed/Expired).

### 2. Service Lifetime in BackgroundService
```csharp
// BackgroundService is singleton, but DbContext is scoped
using var scope = _serviceProvider.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
```

**Critical**: NEVER inject scoped services (DbContext) directly into BackgroundService constructor. Always create scope per operation.

### 3. Testable Randomness
```csharp
// Production
public class PaymentSimulator : IPaymentSimulator
{
    private readonly Random _random = new();
    public bool ShouldCompletePayment() => _random.Next(0, 2) == 1;
}

// Tests
_paymentSimulator.ShouldCompletePayment().Returns(true); // Deterministic
```

**Pattern**: Interface allows mocking in tests while keeping production code simple.

### 4. MassTransit Retry Policy
```csharp
cfg.UseMessageRetry(r => r.Exponential(
    retryLimit: 3,
    minInterval: TimeSpan.FromSeconds(1),
    maxInterval: TimeSpan.FromSeconds(30),
    intervalDelta: TimeSpan.FromSeconds(2)
));
```

**Behavior**:
- Attempt 1: Immediate
- Attempt 2: 1s delay
- Attempt 3: 3s delay
- Attempt 4: 7s delay
- After 3 retries: Message moved to `{queue}_error` queue

### 5. Enum Storage as String
```csharp
entity.Property(e => e.Type).IsRequired()
    .HasConversion<string>()  // Store enum as string in DB
    .HasMaxLength(50);
```

**Why?**: Readable in DB queries, no issues with enum value changes.

### 6. UpdatedAt for Expiration
```csharp
var cutoffTime = DateTime.UtcNow - _expirationThreshold;
var expiredOrders = await dbContext.Orders
    .Where(o => o.Status == OrderStatus.Processing && o.UpdatedAt < cutoffTime)
    .ToListAsync();
```

**Critical**: Use `UpdatedAt` (not CreatedAt) so that orders remain in Processing for 10 minutes after last update, not from creation.

### 7. CancellationToken Usage
```csharp
await Task.Delay(TimeSpan.FromSeconds(5), context.CancellationToken);
```

**Pattern**: Pass CancellationToken to async operations for graceful shutdown support.

### 8. No Try-Catch in Consumers
MassTransit handles exceptions automatically:
- Transient errors: Retry 3 times
- Persistent errors: Move to error queue

**Don't do this**:
```csharp
try {
    // Process message
} catch {
    // Log and swallow
}
```

**Do this**:
```csharp
// Let exception propagate
// MassTransit will retry or move to error queue
```

---

## Validation Gates

### Gate 1: Build Solution
```bash
dotnet build CoderamaOpsAI.sln
# Expected: 0 errors
```

### Gate 2: Run Unit Tests
```bash
dotnet test CoderamaOpsAI.UnitTests
# Expected: All tests pass (existing + new consumer tests)
```

### Gate 3: Apply Migration
```bash
dotnet ef database update --project CoderamaOpsAI.Dal --startup-project CoderamaOpsAI.Api
# Expected: "Applying migration '20YYMMDDHHMMSS_AddNotificationsTable'"
```

### Gate 4: Start Docker Services
```bash
docker-compose down -v  # Clean start
docker-compose up -d --build
# Expected: All 4 services healthy (api, worker, db, rabbitmq)
```

**Verify services**:
```bash
docker-compose ps
# Should show: api (healthy), worker (running), db (healthy), rabbitmq (healthy)
```

### Gate 5: Verify RabbitMQ
1. Navigate to: http://localhost:15672
2. Login: guest / guest
3. Click "Queues" tab
4. Expected queues (created by MassTransit):
   - `order-created-event` (OrderCreatedConsumer)
   - `order-completed-event` (OrderCompletedConsumer)
   - `order-expired-event` (OrderExpiredConsumer)
   - Error queues: `{queue}_error` for each

### Gate 6: End-to-End Test

**Step 1: Login to get JWT token**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"admin123"}'

# Response: {"token":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."}
```

**Step 2: Create order**
```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {TOKEN_FROM_STEP_1}" \
  -d '{"userId":1,"productId":1,"quantity":2,"price":50.0,"status":"Pending"}'

# Response: {"id":1,"userId":1,...,"status":"Pending"}
```

**Step 3: Check Worker logs**
```bash
docker-compose logs -f worker

# Expected logs (within 5 seconds):
# Processing OrderCreated for OrderId=1
# Order 1 status updated to Processing
# (5 seconds later) Order 1 completed and event published (50% chance)
# OR: Order 1 remains in Processing (payment pending) (50% chance)
```

**Step 4: Check notifications** (if order completed)
```bash
curl http://localhost:5000/api/notifications \
  -H "Authorization: Bearer {TOKEN}"

# Expected: List of notifications with OrderCompleted type
```

**Step 5: Test expiration** (for non-completed orders)
- Wait 10+ minutes
- Check Worker logs: "Found 1 expired orders"
- Verify OrderExpired event published
- Check notifications for OrderExpired type

---

## Reference Files & Documentation

### Existing Patterns to Follow
- **DatabaseTestBase**: `CoderamaOpsAI.UnitTests\Common\DatabaseTestBase.cs`
- **Controller pattern**: `CoderamaOpsAI.Api\Controllers\OrdersController.cs`
- **Test pattern**: `CoderamaOpsAI.UnitTests\Api\Controllers\OrdersControllerTests.cs`
- **Order entity**: `CoderamaOpsAI.Dal\Entities\Order.cs` (already has OrderStatus enum)
- **AppDbContext**: `CoderamaOpsAI.Dal\AppDbContext.cs`
- **Docker setup**: `docker-compose.yml`, `CoderamaOpsAI.Api\Dockerfile`

### External Documentation
- **MassTransit RabbitMQ**: https://masstransit.io/documentation/configuration/transports/rabbitmq
- **MassTransit Consumers**: https://masstransit.io/documentation/concepts/consumers
- **MassTransit Retry**: https://masstransit.io/documentation/configuration/middleware/retry
- **BackgroundService**: https://learn.microsoft.com/en-us/dotnet/core/extensions/timer-service
- **EF Core Migrations**: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/

---

## Troubleshooting Guide

| Issue | Diagnosis | Solution |
|-------|-----------|----------|
| Events not consumed | Check RabbitMQ connection | Verify `RabbitMq__Host` in appsettings (should be `"rabbitmq"` in Docker) |
| Worker crashes on startup | Missing DB connection | Ensure `ConnectionStrings__DefaultConnection` in docker-compose.yml |
| Orders not expiring | Job interval too long | Lower `OrderExpiration:IntervalSeconds` to 10 for testing |
| Duplicate notifications | Idempotency not working | Check `AnyAsync` logic, verify notification creation |
| MassTransit registration error | Consumers not added | Ensure consumers registered in `AddEventBus` call |
| Build error: Type not found | Missing using statement | Add `using CoderamaOpsAI.Common.Events;` |
| Docker build fails | Project reference issue | Verify COPY paths in Dockerfile match .csproj locations |
| RabbitMQ out of memory | Too many messages | Check error queues, increase prefetch limit |

---

## Implementation Checklist

### Phase 1: Common Project
- [ ] Add MassTransit NuGet packages (3 packages)
- [ ] Create `OrderCreatedEvent.cs`
- [ ] Create `OrderCompletedEvent.cs`
- [ ] Create `OrderExpiredEvent.cs`
- [ ] Create `IPaymentSimulator.cs` interface
- [ ] Create `PaymentSimulator.cs` implementation
- [ ] Create `IEventBus.cs` interface
- [ ] Create `EventBus.cs` implementation
- [ ] Create `RabbitMqSettings.cs` configuration class
- [ ] Create `MassTransitConfiguration.cs` extension methods

### Phase 2: Database Layer
- [ ] Create `Notification.cs` entity + `NotificationType` enum
- [ ] Update `AppDbContext.cs` (DbSet + configuration)
- [ ] Generate migration: `AddNotificationsTable`
- [ ] Apply migration

### Phase 3: API Integration
- [ ] Add project reference: Api → Common
- [ ] Update `OrdersController.cs` (inject IEventBus, publish event)
- [ ] Update `Api/Program.cs` (register MassTransit)
- [ ] Update `Api/appsettings.json` (add RabbitMq config)

### Phase 4: Worker Consumers
- [ ] Add packages to Worker (MassTransit, EF Core)
- [ ] Add project references (Dal, Common)
- [ ] Create `OrderCreatedConsumer.cs`
- [ ] Create `OrderCompletedConsumer.cs`
- [ ] Create `OrderExpiredConsumer.cs`

### Phase 5: Background Job
- [ ] Create `OrderExpirationJob.cs` BackgroundService

### Phase 6: Worker Setup
- [ ] Change Worker SDK to `Microsoft.NET.Sdk.Worker`
- [ ] Replace `Worker/Program.cs` (Host builder)
- [ ] Create `Worker/appsettings.json`

### Phase 7: Docker Setup
- [ ] Create `Worker/Dockerfile`
- [ ] Update `docker-compose.yml` (add RabbitMQ + Worker services)

### Phase 8: Testing Layer
- [ ] Add MassTransit.TestFramework to UnitTests
- [ ] Create `OrderCreatedConsumerTests.cs` (3 tests)
- [ ] Create `OrderCompletedConsumerTests.cs` (2 tests)
- [ ] Create `OrderExpiredConsumerTests.cs` (2 tests)

### Phase 9: Validation
- [ ] Build: `dotnet build CoderamaOpsAI.sln`
- [ ] Tests: `dotnet test CoderamaOpsAI.UnitTests`
- [ ] Docker: `docker-compose up -d --build`
- [ ] RabbitMQ UI: Verify queues at http://localhost:15672
- [ ] End-to-end: Create order, verify processing, check notifications

---

## Success Criteria

✅ Solution builds without errors
✅ All unit tests pass (existing + 7 new consumer tests)
✅ Notifications migration applied successfully
✅ Docker services all healthy (api, worker, db, rabbitmq)
✅ RabbitMQ queues created automatically by MassTransit
✅ OrderCreated event published when order created
✅ OrderCreatedConsumer processes payment (5s delay)
✅ 50% of orders complete, 50% remain in Processing
✅ OrderCompletedConsumer logs fake email and saves notification
✅ OrderExpirationJob runs every 60 seconds
✅ Processing orders older than 10 minutes expire
✅ OrderExpiredConsumer saves expiration notification
✅ Idempotency prevents duplicate processing/notifications
✅ MassTransit retries 3 times on failure
✅ Failed messages moved to error queues

---

## Notes

- This PRP was generated through comprehensive codebase exploration and planning
- All patterns match existing code conventions (DatabaseTestBase, Given_When_Then naming)
- MassTransit configuration based on official documentation
- Status-based idempotency chosen for simplicity (sufficient for this use case)
- Simple consumers preferred over saga orchestration (events are mostly independent)
- Worker SDK chosen for semantic correctness of background services
- Testing strategy follows existing patterns (mock dependencies, in-memory DB)
- Docker setup extends existing infrastructure (adds RabbitMQ + Worker)
- Total implementation: 34 new files, 5 modified files, 1 migration
- Estimated confidence score: 9/10 for one-pass implementation success
