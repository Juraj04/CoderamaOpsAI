# PRP: CRUD API Endpoints for Users, Products, and Orders

## Confidence Score: 9/10
This PRP provides comprehensive context for one-pass implementation with exact specifications, validation gates, and patterns discovered from codebase exploration.

---

## Overview

Implement complete CRUD functionality for three modules:
- **Users**: CRUD endpoints for existing Users table (add audit fields)
- **Products**: New entity, table, migration, and CRUD endpoints
- **Orders**: New entity with FK relationships to Users/Products, and CRUD endpoints

All endpoints require JWT authentication, include Swagger documentation, use DTOs, and have comprehensive unit tests.

---

## Technology Context

- **.NET 8** with PostgreSQL + EF Core
- **Authentication**: Custom JWT (no ASP.NET Identity) with BCrypt password hashing
- **Testing**: xUnit + FluentAssertions + NSubstitute + In-Memory Database
- **API Style**: Controllers-based with [Authorize] attributes
- **Migration Pattern**: Code-first with seed data in Up() methods

---

## Implementation Order

### Phase 1: Database Layer (Entities + Migrations)
1. Update User entity with audit fields
2. Create Product entity
3. Create Order entity with OrderStatus enum
4. Update AppDbContext configuration
5. Generate and edit migrations with seed data

### Phase 2: API Layer (DTOs + Controllers)
6. Create DTOs for all three modules
7. Implement UsersController with CRUD
8. Implement ProductsController with CRUD
9. Implement OrdersController with CRUD + FK validation

### Phase 3: Testing Layer
10. Create UsersControllerTests
11. Create ProductsControllerTests
12. Create OrdersControllerTests

### Phase 4: Validation
13. Build solution
14. Run all unit tests
15. Apply migrations via Docker
16. Test endpoints via Swagger

---

## Entity Specifications

### User Entity (Updated)
**File**: `CoderamaOpsAI.Dal\Entities\User.cs`

```csharp
namespace CoderamaOpsAI.Dal.Entities;

public class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public DateTime CreatedAt { get; set; }      // NEW
    public DateTime UpdatedAt { get; set; }      // NEW
    public ICollection<Order> Orders { get; set; } = new List<Order>();  // NEW
}
```

### Product Entity (New)
**File**: `CoderamaOpsAI.Dal\Entities\Product.cs`

```csharp
namespace CoderamaOpsAI.Dal.Entities;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
```

### Order Entity (New)
**File**: `CoderamaOpsAI.Dal\Entities\Order.cs`

```csharp
namespace CoderamaOpsAI.Dal.Entities;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Expired
}
```

---

## AppDbContext Configuration

**File**: `CoderamaOpsAI.Dal\AppDbContext.cs`

**Add DbSets**:
```csharp
public DbSet<Product> Products => Set<Product>();
public DbSet<Order> Orders => Set<Order>();
```

**Add to OnModelCreating**:

```csharp
// Product configuration
modelBuilder.Entity<Product>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).ValueGeneratedOnAdd();
    entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
    entity.Property(e => e.Description).HasMaxLength(500);
    entity.Property(e => e.Price).IsRequired().HasColumnType("decimal(18,2)");
    entity.Property(e => e.Stock).IsRequired();
    entity.Property(e => e.CreatedAt).IsRequired();
});

// Order configuration
modelBuilder.Entity<Order>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).ValueGeneratedOnAdd();
    entity.Property(e => e.Quantity).IsRequired();
    entity.Property(e => e.Price).IsRequired().HasColumnType("decimal(18,2)");
    entity.Property(e => e.Total).IsRequired().HasColumnType("decimal(18,2)");
    entity.Property(e => e.Status).IsRequired()
        .HasConversion<string>()  // Store enum as string in DB
        .HasMaxLength(20);
    entity.Property(e => e.CreatedAt).IsRequired();
    entity.Property(e => e.UpdatedAt).IsRequired();

    // Foreign key relationships
    entity.HasOne(e => e.User)
        .WithMany(u => u.Orders)
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne(e => e.Product)
        .WithMany(p => p.Orders)
        .HasForeignKey(e => e.ProductId)
        .OnDelete(DeleteBehavior.Restrict);
});

// Update User configuration (add audit fields)
modelBuilder.Entity<User>(entity =>
{
    // ... existing configuration ...
    entity.Property(e => e.CreatedAt).IsRequired();
    entity.Property(e => e.UpdatedAt).IsRequired();
});
```

---

## Migration Strategy

**Create migrations** (run from solution root):
```bash
dotnet ef migrations add AddProductsTable --project CoderamaOpsAI.Dal --startup-project CoderamaOpsAI.Api
dotnet ef migrations add AddOrdersTable --project CoderamaOpsAI.Dal --startup-project CoderamaOpsAI.Api
dotnet ef migrations add AddAuditFieldsToUser --project CoderamaOpsAI.Dal --startup-project CoderamaOpsAI.Api
```

**Seed data pattern** (add to migration Up() methods):

**Products seed data**:
```csharp
migrationBuilder.InsertData(
    table: "Products",
    columns: new[] { "Name", "Description", "Price", "Stock", "CreatedAt" },
    values: new object[] { "Laptop Pro", "High-performance laptop", 1299.99m, 50, DateTime.UtcNow });

migrationBuilder.InsertData(
    table: "Products",
    columns: new[] { "Name", "Description", "Price", "Stock", "CreatedAt" },
    values: new object[] { "Wireless Mouse", "Ergonomic wireless mouse", 29.99m, 200, DateTime.UtcNow });

migrationBuilder.InsertData(
    table: "Products",
    columns: new[] { "Name", "Description", "Price", "Stock", "CreatedAt" },
    values: new object[] { "USB-C Cable", "3-meter USB-C cable", 9.99m, 500, DateTime.UtcNow });
```

**Orders seed data**:
```csharp
migrationBuilder.InsertData(
    table: "Orders",
    columns: new[] { "UserId", "ProductId", "Quantity", "Price", "Total", "Status", "CreatedAt", "UpdatedAt" },
    values: new object[] { 1, 1, 2, 1299.99m, 2599.98m, "Completed", DateTime.UtcNow, DateTime.UtcNow });

migrationBuilder.InsertData(
    table: "Orders",
    columns: new[] { "UserId", "ProductId", "Quantity", "Price", "Total", "Status", "CreatedAt", "UpdatedAt" },
    values: new object[] { 2, 2, 1, 29.99m, 29.99m, "Pending", DateTime.UtcNow, DateTime.UtcNow });
```

---

## DTO Specifications

### User DTOs
**File**: `CoderamaOpsAI.Api\Models\UserDtos.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace CoderamaOpsAI.Api.Models;

public class CreateUserRequest
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public required string Password { get; set; }
}

public class UpdateUserRequest
{
    [MaxLength(100)] public string? Name { get; set; }
    [EmailAddress] [MaxLength(100)] public string? Email { get; set; }
    [MinLength(6)] public string? Password { get; set; }
}

public class UserResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // NEVER include Password
}
```

### Product DTOs
**File**: `CoderamaOpsAI.Api\Models\ProductDtos.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace CoderamaOpsAI.Api.Models;

public class CreateProductRequest
{
    [Required] [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required] [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required] [Range(0, int.MaxValue)]
    public int Stock { get; set; }
}

public class UpdateProductRequest
{
    [MaxLength(100)] public string? Name { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    [Range(0, double.MaxValue)] public decimal? Price { get; set; }
    [Range(0, int.MaxValue)] public int? Stock { get; set; }
}

public class ProductResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Order DTOs
**File**: `CoderamaOpsAI.Api\Models\OrderDtos.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using CoderamaOpsAI.Dal.Entities;

namespace CoderamaOpsAI.Api.Models;

public class CreateOrderRequest
{
    [Required] public int UserId { get; set; }
    [Required] public int ProductId { get; set; }
    [Required] [Range(1, int.MaxValue)] public int Quantity { get; set; }
    [Required] [Range(0.01, double.MaxValue)] public decimal Price { get; set; }
    [Required] public OrderStatus Status { get; set; }
}

public class UpdateOrderRequest
{
    public int? UserId { get; set; }
    public int? ProductId { get; set; }
    [Range(1, int.MaxValue)] public int? Quantity { get; set; }
    [Range(0.01, double.MaxValue)] public decimal? Price { get; set; }
    public OrderStatus? Status { get; set; }
}

public class OrderResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

## Controller Patterns

### Common Controller Structure
**Reference**: `CoderamaOpsAI.Api\Controllers\AuthController.cs`

**Pattern to follow**:
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // All endpoints require authentication
public class UsersController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UsersController> _logger;

    public UsersController(AppDbContext dbContext, ILogger<UsersController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users
            .AsNoTracking()  // Read-only optimization
            .ToListAsync(cancellationToken);

        // Map to DTOs (exclude password)
        var response = users.Select(u => new UserResponse
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        }).ToList();

        return Ok(response);
    }

    // ... more endpoints
}
```

### UsersController Endpoints
**File**: `CoderamaOpsAI.Api\Controllers\UsersController.cs`

1. **GET /api/users** - Get all users (exclude passwords)
2. **GET /api/users/{id}** - Get user by ID (404 if not found)
3. **POST /api/users** - Create user (hash password, set audit fields)
4. **PUT /api/users/{id}** - Update user (hash password if changed, update UpdatedAt)
5. **DELETE /api/users/{id}** - Delete user (404 if not found)

**Critical pattern - Password hashing**:
```csharp
// On Create
var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
user.Password = hashedPassword;
user.CreatedAt = DateTime.UtcNow;
user.UpdatedAt = DateTime.UtcNow;

// On Update (if password provided)
if (!string.IsNullOrWhiteSpace(request.Password))
{
    user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
}
user.UpdatedAt = DateTime.UtcNow;
```

### ProductsController Endpoints
**File**: `CoderamaOpsAI.Api\Controllers\ProductsController.cs`

1. **GET /api/products** - Get all products
2. **GET /api/products/{id}** - Get product by ID (404 if not found)
3. **POST /api/products** - Create product (set CreatedAt)
4. **PUT /api/products/{id}** - Update product
5. **DELETE /api/products/{id}** - Delete product (404 if not found)

**Validation**: Price >= 0, Stock >= 0 (handled by DTO attributes)

### OrdersController Endpoints
**File**: `CoderamaOpsAI.Api\Controllers\OrdersController.cs`

1. **GET /api/orders** - Get all orders with User.Name and Product.Name
2. **GET /api/orders/{id}** - Get order by ID with relations (404 if not found)
3. **POST /api/orders** - Create order (validate FKs, calculate total, set audit fields)
4. **PUT /api/orders/{id}** - Update order (validate FKs, recalculate total, update UpdatedAt)
5. **DELETE /api/orders/{id}** - Delete order (404 if not found)

**Critical patterns**:

**FK Validation**:
```csharp
var userExists = await _dbContext.Users
    .AnyAsync(u => u.Id == request.UserId, cancellationToken);
if (!userExists)
    return BadRequest(new { message = "User not found" });

var productExists = await _dbContext.Products
    .AnyAsync(p => p.Id == request.ProductId, cancellationToken);
if (!productExists)
    return BadRequest(new { message = "Product not found" });
```

**Total Calculation**:
```csharp
// On Create
order.Total = order.Quantity * order.Price;

// On Update (if Quantity or Price changed)
if (request.Quantity.HasValue || request.Price.HasValue)
{
    var qty = request.Quantity ?? order.Quantity;
    var price = request.Price ?? order.Price;
    order.Total = qty * price;
}
```

**Include Related Data**:
```csharp
var orders = await _dbContext.Orders
    .Include(o => o.User)
    .Include(o => o.Product)
    .AsNoTracking()
    .ToListAsync(cancellationToken);

// Map to response with names
var response = new OrderResponse
{
    // ... other fields ...
    UserName = order.User.Name,
    ProductName = order.Product.Name,
    Status = order.Status.ToString()
};
```

---

## Testing Strategy

### Test Base Class
**Reference**: `CoderamaOpsAI.UnitTests\Common\DatabaseTestBase.cs`

All controller tests inherit from `DatabaseTestBase` which provides:
- In-memory database (unique per test via Guid)
- Protected `DbContext` property
- Automatic disposal

### Test Structure Pattern
**Reference**: `CoderamaOpsAI.UnitTests\Api\Controllers\AuthControllerTests.cs`

```csharp
public class UsersControllerTests : DatabaseTestBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _logger = Substitute.For<ILogger<UsersController>>();
        _controller = new UsersController(DbContext, _logger);
    }

    [Fact]
    public async Task GetAll_WithUsers_ReturnsOkWithUserList()
    {
        // Arrange
        var user = new User
        {
            Name = "Test",
            Email = "test@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as List<UserResponse>;
        response.Should().NotBeNull();
        response.Should().HaveCount(1);
    }
}
```

### Test Cases Required

**UsersControllerTests** (16 tests):
- GetAll with/without users
- GetById existing/non-existent
- Create with valid data, verify hashing, verify audit fields
- Create with duplicate email (400)
- Update existing/non-existent
- Update with new password (verify hashing)
- Update updates UpdatedAt
- Delete existing/non-existent
- Verify AsNoTracking used

**ProductsControllerTests** (13 tests):
- GetAll with/without products
- GetById existing/non-existent
- Create with valid data, verify CreatedAt
- Create with negative price/stock (validation)
- Update existing/non-existent
- Delete existing/non-existent
- Verify AsNoTracking used

**OrdersControllerTests** (18 tests):
- GetAll with/without orders, verify relations included
- GetById existing/non-existent, verify relations
- Create with valid data, verify total calculation, verify audit fields
- Create with non-existent User/Product FK (400)
- Create with zero quantity/price (validation)
- Update existing/non-existent
- Update recalculates total
- Update updates UpdatedAt
- Delete existing/non-existent
- Verify related names included in response

---

## Critical Gotchas & Patterns

### 1. Password Security
- **ALWAYS** hash with BCrypt before saving
- **NEVER** return password in response DTOs
- Use `BCrypt.Net.BCrypt.HashPassword()` for hashing
- Use `BCrypt.Net.BCrypt.Verify()` for verification

### 2. Audit Fields
```csharp
// On Create
entity.CreatedAt = DateTime.UtcNow;
entity.UpdatedAt = DateTime.UtcNow;

// On Update
entity.UpdatedAt = DateTime.UtcNow;
// NEVER modify CreatedAt on update
```

### 3. Read-Only Queries
Always use `.AsNoTracking()` for GET operations:
```csharp
var users = await _dbContext.Users.AsNoTracking().ToListAsync();
```

### 4. Enum Storage
Store enums as strings using Fluent API:
```csharp
entity.Property(e => e.Status)
    .HasConversion<string>()
    .HasMaxLength(20);
```

### 5. NotFound Pattern
```csharp
var entity = await _dbContext.Users.FindAsync(id);
if (entity == null)
    return NotFound(new { message = "User not found" });
```

### 6. No Try-Catch in Controllers
Global exception middleware handles all errors. Controllers should NOT use try-catch blocks.

### 7. Migration Order
1. AddProductsTable (with seed data)
2. AddOrdersTable (with seed data - depends on Users + Products)
3. AddAuditFieldsToUser (updates existing table)

### 8. Partial Updates
Only update provided fields:
```csharp
if (!string.IsNullOrWhiteSpace(request.Name))
    user.Name = request.Name;
```

---

## Validation Gates

### Build
```bash
dotnet build CoderamaOpsAI.sln
# Expected: 0 errors
```

### Unit Tests
```bash
dotnet test CoderamaOpsAI.UnitTests
# Expected: All tests pass (green)

# Run specific test class
dotnet test --filter "FullyQualifiedName~UsersControllerTests"
```

### Migrations & Docker
```bash
# Clean start
docker-compose down -v
docker-compose up -d --build

# Verify migrations applied
docker-compose logs -f api
# Look for: "Applied migration 'AddProductsTable'"
```

### API Testing
1. Start: `docker-compose up -d`
2. Navigate to: http://localhost:5000/swagger
3. Login with: `admin@example.com` / `Admin123!`
4. Copy JWT token
5. Click "Authorize" button, enter: `Bearer <token>`
6. Test all endpoints (GET, POST, PUT, DELETE)

### Database Verification
```bash
docker exec -it coderamaopsai-db-1 psql -U postgres -d coderamaopsai
\dt                           # List tables
SELECT * FROM "Products";     # Verify seed data
SELECT * FROM "Orders";       # Verify seed data
\q                            # Exit
```

---

## Reference Files

### Patterns to Follow
- **Controller pattern**: `CoderamaOpsAI.Api\Controllers\AuthController.cs`
- **DTO pattern**: `CoderamaOpsAI.Api\Models\LoginRequest.cs`
- **Entity pattern**: `CoderamaOpsAI.Dal\Entities\User.cs`
- **DbContext pattern**: `CoderamaOpsAI.Dal\AppDbContext.cs`
- **Migration pattern**: `CoderamaOpsAI.Dal\Migrations\20251124145633_InitialCreate.cs`
- **Test base**: `CoderamaOpsAI.UnitTests\Common\DatabaseTestBase.cs`
- **Test pattern**: `CoderamaOpsAI.UnitTests\Api\Controllers\AuthControllerTests.cs`

### External Documentation
- **EF Core Migrations**: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- **BCrypt.NET**: https://github.com/BcryptNet/bcrypt.net
- **FluentAssertions**: https://fluentassertions.com/introduction
- **NSubstitute**: https://nsubstitute.github.io/help/getting-started/

---

## Implementation Checklist

### Database Layer
- [ ] Update `User.cs` entity (audit fields, Orders navigation)
- [ ] Create `Product.cs` entity
- [ ] Create `Order.cs` entity + `OrderStatus` enum
- [ ] Update `AppDbContext.cs` (DbSets + Fluent API config)
- [ ] Generate migration: `AddProductsTable`
- [ ] Edit migration: Add Products seed data
- [ ] Generate migration: `AddOrdersTable`
- [ ] Edit migration: Add Orders seed data
- [ ] Generate migration: `AddAuditFieldsToUser`
- [ ] Edit `InitialCreate` migration: Update User seeds with audit fields

### API Layer
- [ ] Create `UserDtos.cs` (3 classes)
- [ ] Create `ProductDtos.cs` (3 classes)
- [ ] Create `OrderDtos.cs` (3 classes)
- [ ] Create `UsersController.cs` (5 endpoints, [Authorize])
- [ ] Create `ProductsController.cs` (5 endpoints, [Authorize])
- [ ] Create `OrdersController.cs` (5 endpoints, [Authorize], FK validation)

### Testing Layer
- [ ] Create `UsersControllerTests.cs` (16 tests)
- [ ] Create `ProductsControllerTests.cs` (13 tests)
- [ ] Create `OrdersControllerTests.cs` (18 tests)

### Validation
- [ ] Build: `dotnet build CoderamaOpsAI.sln`
- [ ] Tests: `dotnet test CoderamaOpsAI.UnitTests`
- [ ] Docker: `docker-compose up -d --build`
- [ ] Swagger: Test all endpoints at http://localhost:5000/swagger

---

## Success Criteria

✅ All 47 unit tests pass
✅ Solution builds without errors
✅ Migrations apply successfully
✅ Seed data appears in database
✅ All endpoints work in Swagger with JWT auth
✅ Passwords are hashed (never plain text)
✅ No passwords in response DTOs
✅ Foreign key constraints enforced
✅ Audit fields set correctly
✅ Enum stored as string in database

---

## Notes

- This PRP was generated through comprehensive codebase exploration
- All patterns match existing code conventions (AuthController, DatabaseTestBase)
- BCrypt hashing follows existing Login implementation
- JWT authentication already configured in Program.cs
- Global exception middleware already configured (no try-catch needed)
- Total calculation is server-side (client sends quantity + price)
- Three separate migrations maintain clean separation of concerns
