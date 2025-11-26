# Integration Test Coverage Analysis

**Generated:** 2025-11-26
**Project:** CoderamaOpsAI

---

## Executive Summary

Current integration test coverage focuses primarily on the order processing workflow with worker integration. However, several critical areas lack integration testing, including authentication flows, CRUD operations for Products and Users, notification system verification, and error handling scenarios.

**Current Test Count:** 2 integration tests
**Recommended Minimum:** 7+ integration tests
**Priority Areas:** Authentication, CRUD operations, Error handling, Notification system, Background jobs

---

## Current Integration Test Coverage

### Location
`CoderamaOpsAI.IntegrationTests/OrderProcessingIntegrationTests.cs`

### Existing Tests

#### 1. `Given_OrderCreated_When_WorkerProcesses_Then_StatusChangesFromPending`
- **What it tests:** End-to-end order creation and worker processing
- **Coverage:**
  - JWT authentication (login)
  - Order creation via API
  - OrderCreatedEvent publishing to RabbitMQ
  - Worker consumption and processing
  - Database state verification
  - Status transition from Pending to Processing/Completed
- **Duration:** ~20 seconds (includes polling for status change)
- **Technologies verified:**
  - PostgreSQL (Testcontainers)
  - RabbitMQ (Testcontainers)
  - MassTransit messaging
  - EF Core
  - JWT authentication

#### 2. `Given_OrderCreated_When_WorkerProcesses_Then_OrderCompletedOrProcessing`
- **What it tests:** Order processing final state verification
- **Coverage:**
  - Similar to test #1 but with fixed wait time
  - Verifies UpdatedAt timestamp changes
  - Verifies final status is Processing or Completed (not Pending)
- **Duration:** ~7 seconds (fixed delay)

### Infrastructure
- **Base class:** `IntegrationTestBase`
- **Test containers:** PostgreSQL, RabbitMQ
- **Services started:**
  - API (WebApplicationFactory)
  - Worker (IHost with background services)
- **Database:** Migrations applied automatically
- **Message bus:** MassTransit with RabbitMQ

---

## Coverage Gaps Identified

### 🔴 Critical Gaps (Must Have)

1. **Authentication & Authorization**
   - ❌ No tests for invalid login credentials
   - ❌ No tests for expired JWT tokens
   - ❌ No tests for unauthorized endpoint access (missing token)
   - ❌ No tests for token structure validation
   - ❌ No tests for password security (BCrypt hashing)

2. **Products CRUD Operations**
   - ❌ No integration tests for GET /api/products
   - ❌ No integration tests for POST /api/products (create)
   - ❌ No integration tests for GET /api/products/{id} (details)
   - ❌ No integration tests for PUT /api/products/{id} (update)
   - ❌ No integration tests for DELETE /api/products/{id}
   - ❌ No validation testing (max length, price range, stock)

3. **Users CRUD Operations**
   - ❌ No integration tests for user endpoints
   - ❌ No tests for user creation with password hashing
   - ❌ No tests for duplicate email prevention

4. **Order Expiration Flow**
   - ❌ No tests for OrderExpirationJob background service
   - ❌ No tests verifying orders expire after 10 minutes of Processing
   - ❌ No tests for OrderExpiredEvent publishing
   - ❌ No tests for OrderExpiredConsumer

5. **Notification System**
   - ❌ No tests verifying notification creation on OrderCompleted
   - ❌ No tests verifying notification creation on OrderExpired
   - ❌ No tests for idempotency (duplicate notifications prevented)
   - ❌ No tests for notification metadata (JSON structure)

### 🟡 Important Gaps (Should Have)

6. **Error Handling & Validation**
   - ❌ No tests for 404 Not Found scenarios
   - ❌ No tests for 400 Bad Request (validation failures)
   - ❌ No tests for foreign key constraint violations
   - ❌ No tests for invalid request bodies
   - ❌ No tests for malformed JSON

7. **Concurrent Operations**
   - ❌ No tests for multiple simultaneous order creations
   - ❌ No tests for race conditions in stock management
   - ❌ No tests for concurrent worker processing

8. **Complex Business Scenarios**
   - ❌ No tests for order filtering by user (users see only their orders)
   - ❌ No tests for product stock depletion
   - ❌ No tests for order total calculation verification

### 🟢 Nice to Have Gaps

9. **Performance & Scalability**
   - ❌ No load tests for API endpoints
   - ❌ No tests for bulk operations
   - ❌ No tests for database query performance (N+1 queries)

10. **API Contract Validation**
    - ❌ No tests verifying OpenAPI/Swagger schema compliance
    - ❌ No tests for DTOs matching entity structure

---

## Recommended New Integration Tests

Below are 10 new integration test cases prioritized by importance. Implement at least the first 5 (Critical Priority).

---

### Test #1: Authentication Flow - Valid and Invalid Credentials
**Priority:** 🔴 Critical
**File:** `CoderamaOpsAI.IntegrationTests/Api/AuthenticationIntegrationTests.cs`
**Estimated Complexity:** Low

#### Test Case: `Given_ValidCredentials_When_Login_Then_ReturnsTokenWithCorrectStructure`

**Purpose:** Verify authentication flow returns valid JWT token with correct claims

**Arrange:**
```csharp
var validLoginRequest = new LoginRequest
{
    Email = "test@example.com",
    Password = "Test123!"
};
```

**Act:**
```csharp
var response = await ApiClient.PostAsJsonAsync("/api/auth/login", validLoginRequest);
var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>();
```

**Assert:**
- Response status is 200 OK
- Token is not null or empty
- ExpiresAt is approximately 10 minutes in the future (±30 seconds tolerance)
- Email matches request email
- Name is populated
- Token can be decoded and contains:
  - `sub` claim with user ID
  - `email` claim
  - `name` claim
  - `iss` claim matches configuration
  - `aud` claim matches configuration
  - `exp` claim matches ExpiresAt

**Why this matters:**
- Verifies JWT generation is working correctly
- Confirms token expiration configuration (10 minutes per AuthController.cs:60)
- Ensures claims are properly set for downstream authorization

---

#### Test Case: `Given_InvalidPassword_When_Login_Then_ReturnsUnauthorized`

**Purpose:** Verify login fails with incorrect password

**Arrange:**
```csharp
var invalidLoginRequest = new LoginRequest
{
    Email = "test@example.com",
    Password = "WrongPassword123!"
};
```

**Act:**
```csharp
var response = await ApiClient.PostAsJsonAsync("/api/auth/login", invalidLoginRequest);
```

**Assert:**
- Response status is 401 Unauthorized
- Response body contains generic message "Invalid email or password"
- No token is returned
- Response does not reveal whether email exists (prevents email enumeration per AuthController.cs:47)

**Why this matters:**
- Verifies BCrypt password verification is working
- Ensures security best practice of generic error messages
- Prevents user enumeration attacks

---

#### Test Case: `Given_NonExistentEmail_When_Login_Then_ReturnsUnauthorized`

**Purpose:** Verify login fails with non-existent email

**Arrange:**
```csharp
var invalidLoginRequest = new LoginRequest
{
    Email = "nonexistent@example.com",
    Password = "AnyPassword123!"
};
```

**Act:**
```csharp
var response = await ApiClient.PostAsJsonAsync("/api/auth/login", invalidLoginRequest);
```

**Assert:**
- Response status is 401 Unauthorized
- Response body contains same generic message as invalid password test
- No token is returned

**Why this matters:**
- Prevents email enumeration attacks
- Verifies consistent error handling for authentication failures

---

### Test #2: Protected Endpoint Access Without Authentication
**Priority:** 🔴 Critical
**File:** `CoderamaOpsAI.IntegrationTests/Api/AuthorizationIntegrationTests.cs`
**Estimated Complexity:** Low

#### Test Case: `Given_NoAuthToken_When_AccessProtectedEndpoint_Then_ReturnsUnauthorized`

**Purpose:** Verify protected endpoints reject requests without JWT tokens

**Arrange:**
```csharp
// Do NOT set Authorization header
ApiClient.DefaultRequestHeaders.Authorization = null;

var protectedEndpoints = new[]
{
    "/api/products",
    "/api/orders",
    "/api/users"
};
```

**Act & Assert:**
```csharp
foreach (var endpoint in protectedEndpoints)
{
    var response = await ApiClient.GetAsync(endpoint);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    response.Headers.WwwAuthenticate.Should().NotBeNull();
}
```

**Why this matters:**
- Verifies `[Authorize]` attribute is working on all controllers
- Ensures no endpoints are accidentally left unprotected
- Confirms JWT authentication middleware is properly configured

---

#### Test Case: `Given_ExpiredToken_When_AccessProtectedEndpoint_Then_ReturnsUnauthorized`

**Purpose:** Verify expired tokens are rejected

**Arrange:**
```csharp
// Create a token with ExpiresAt in the past
var expiredToken = _jwtTokenService.GenerateToken(testUser, DateTime.UtcNow.AddMinutes(-1));
ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);
```

**Act:**
```csharp
var response = await ApiClient.GetAsync("/api/products");
```

**Assert:**
- Response status is 401 Unauthorized
- Response indicates token is expired

**Why this matters:**
- Critical for frontend requirement: "must handle Unauthorized response correctly, in case token expires" (INITIAL_FE.md:24)
- Ensures token expiration is enforced (10-minute timeout per AuthController.cs:60)
- Validates JWT validation middleware configuration

---

### Test #3: Products CRUD Operations End-to-End
**Priority:** 🔴 Critical
**File:** `CoderamaOpsAI.IntegrationTests/Api/ProductsIntegrationTests.cs`
**Estimated Complexity:** Medium

#### Test Case: `Given_AuthenticatedUser_When_PerformProductsCRUD_Then_AllOperationsSucceed`

**Purpose:** Comprehensive test of full product lifecycle

**Arrange:**
```csharp
// Login to get token
var loginResponse = await ApiClient.PostAsJsonAsync("/api/auth/login", validCredentials);
var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);
```

**Act & Assert (Step-by-step):**

**Step 1: List all products (GET /api/products)**
```csharp
var listResponse = await ApiClient.GetAsync("/api/products");
listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

var products = await listResponse.Content.ReadFromJsonAsync<List<ProductResponse>>();
products.Should().NotBeNull();
var initialCount = products!.Count;
```

**Step 2: Create new product (POST /api/products)**
```csharp
var createRequest = new CreateProductRequest
{
    Name = "Integration Test Product",
    Description = "Created during integration test",
    Price = 129.99m,
    Stock = 50
};

var createResponse = await ApiClient.PostAsJsonAsync("/api/products", createRequest);
createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
createdProduct.Should().NotBeNull();
createdProduct!.Name.Should().Be(createRequest.Name);
createdProduct.Price.Should().Be(createRequest.Price);
createdProduct.Stock.Should().Be(createRequest.Stock);
createdProduct.Id.Should().BeGreaterThan(0);
createdProduct.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

// Verify Location header
createResponse.Headers.Location.Should().NotBeNull();
createResponse.Headers.Location!.ToString().Should().Contain($"/api/products/{createdProduct.Id}");
```

**Step 3: Get product by ID (GET /api/products/{id})**
```csharp
var getResponse = await ApiClient.GetAsync($"/api/products/{createdProduct.Id}");
getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

var retrievedProduct = await getResponse.Content.ReadFromJsonAsync<ProductResponse>();
retrievedProduct.Should().BeEquivalentTo(createdProduct);
```

**Step 4: Update product (PUT /api/products/{id})**
```csharp
var updateRequest = new UpdateProductRequest
{
    Name = "Updated Integration Test Product",
    Price = 149.99m,
    Stock = 75
};

var updateResponse = await ApiClient.PutAsJsonAsync($"/api/products/{createdProduct.Id}", updateRequest);
updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

var updatedProduct = await updateResponse.Content.ReadFromJsonAsync<ProductResponse>();
updatedProduct!.Name.Should().Be(updateRequest.Name);
updatedProduct.Price.Should().Be(updateRequest.Price);
updatedProduct.Stock.Should().Be(updateRequest.Stock);
updatedProduct.Id.Should().Be(createdProduct.Id); // ID unchanged
updatedProduct.CreatedAt.Should().Be(createdProduct.CreatedAt); // CreatedAt unchanged
```

**Step 5: Verify in database directly**
```csharp
using var dbContext = GetDbContext();
var dbProduct = await dbContext.Products
    .AsNoTracking()
    .FirstOrDefaultAsync(p => p.Id == createdProduct.Id);

dbProduct.Should().NotBeNull();
dbProduct!.Name.Should().Be(updateRequest.Name);
dbProduct.Price.Should().Be(updateRequest.Price);
```

**Step 6: List all products again, verify count increased**
```csharp
var listResponse2 = await ApiClient.GetAsync("/api/products");
var products2 = await listResponse2.Content.ReadFromJsonAsync<List<ProductResponse>>();
products2!.Count.Should().Be(initialCount + 1);
products2.Should().Contain(p => p.Id == createdProduct.Id);
```

**Step 7: Delete product (DELETE /api/products/{id})**
```csharp
var deleteResponse = await ApiClient.DeleteAsync($"/api/products/{createdProduct.Id}");
deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
```

**Step 8: Verify deleted (GET returns 404)**
```csharp
var getDeletedResponse = await ApiClient.GetAsync($"/api/products/{createdProduct.Id}");
getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
```

**Step 9: Verify in database - product should not exist**
```csharp
using var dbContext2 = GetDbContext();
var deletedProduct = await dbContext2.Products
    .AsNoTracking()
    .FirstOrDefaultAsync(p => p.Id == createdProduct.Id);

deletedProduct.Should().BeNull();
```

**Why this matters:**
- Verifies complete product lifecycle in a single atomic test
- Tests all CRUD operations work together correctly
- Verifies API responses match database state
- Ensures Location headers are correct (REST best practice)
- Confirms soft-delete is NOT used (entity truly deleted)
- Tests referenced in INITIAL_FE.md:14 "list products, add new product, get product detail on click"

---

#### Test Case: `Given_InvalidProductData_When_Create_Then_ReturnsValidationError`

**Purpose:** Verify validation rules are enforced

**Arrange:**
```csharp
// Login first
var token = await GetAuthToken();
ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

var invalidRequests = new[]
{
    new {
        Request = new CreateProductRequest { Name = "", Price = 10m, Stock = 5 },
        ExpectedError = "Name is required"
    },
    new {
        Request = new CreateProductRequest { Name = "Test", Price = -10m, Stock = 5 },
        ExpectedError = "Price must be >= 0"
    },
    new {
        Request = new CreateProductRequest { Name = new string('x', 101), Price = 10m, Stock = 5 },
        ExpectedError = "Name must not exceed 100 characters"
    }
};
```

**Act & Assert:**
```csharp
foreach (var testCase in invalidRequests)
{
    var response = await ApiClient.PostAsJsonAsync("/api/products", testCase.Request);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var errorContent = await response.Content.ReadAsStringAsync();
    errorContent.Should().Contain("validation");
}
```

**Why this matters:**
- Verifies DataAnnotations validation from ProductDtos.cs:1-21
- Ensures bad data cannot enter the system
- Tests API returns proper 400 Bad Request with validation details

---

### Test #4: Notification System Verification
**Priority:** 🔴 Critical
**File:** `CoderamaOpsAI.IntegrationTests/Worker/NotificationIntegrationTests.cs`
**Estimated Complexity:** Medium-High

#### Test Case: `Given_OrderCompleted_When_EventProcessed_Then_NotificationCreated`

**Purpose:** Verify end-to-end notification creation flow

**Arrange:**
```csharp
// Login and create order
var token = await GetAuthToken();
ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

using var dbContext = GetDbContext();
var testUser = await dbContext.Users.FirstAsync();
var testProduct = await dbContext.Products.FirstAsync();

var createOrderRequest = new CreateOrderRequest
{
    UserId = testUser.Id,
    ProductId = testProduct.Id,
    Quantity = 3,
    Price = 79.99m,
    Status = OrderStatus.Pending
};
```

**Act:**
```csharp
// Create order
var createResponse = await ApiClient.PostAsJsonAsync("/api/orders", createOrderRequest);
var orderResponse = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
var orderId = orderResponse!.Id;

// Wait for worker to process order to Completed state
await Task.Delay(TimeSpan.FromSeconds(10));

// Manually publish OrderCompletedEvent (to guarantee completion for test)
var eventBus = GetEventBus();
await eventBus.PublishAsync(new OrderCompletedEvent(
    orderId,
    testUser.Id,
    orderResponse.Total
));

// Wait for OrderCompletedConsumer to process
await Task.Delay(TimeSpan.FromSeconds(3));
```

**Assert:**
```csharp
// Verify notification was created in database
using var checkDbContext = GetDbContext();
var notification = await checkDbContext.Notifications
    .AsNoTracking()
    .FirstOrDefaultAsync(n => n.OrderId == orderId && n.Type == NotificationType.OrderCompleted);

notification.Should().NotBeNull();
notification!.Message.Should().Contain($"Order #{orderId}");
notification.Message.Should().Contain("completed successfully");
notification.Metadata.Should().NotBeNullOrEmpty();

// Verify metadata JSON structure
var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(notification.Metadata!);
metadata.Should().ContainKey("UserId");
metadata.Should().ContainKey("Total");

notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(30));
```

**Why this matters:**
- Verifies Notification entity is correctly populated (Notification.cs)
- Tests OrderCompletedConsumer (OrderCompletedConsumer.cs:22-58)
- Confirms metadata JSON serialization works
- Verifies idempotency check (consumer doesn't create duplicate notifications per OrderCompletedConsumer.cs:32-35)
- Critical for feature requirement: "create order with some notification that order was created" (INITIAL_FE.md:16)

---

#### Test Case: `Given_DuplicateOrderCompletedEvent_When_Consumed_Then_OnlyOneNotification`

**Purpose:** Verify idempotency of notification creation

**Arrange:**
```csharp
var orderId = 12345;
var userId = 1;
var total = 299.99m;

var orderCompletedEvent = new OrderCompletedEvent(orderId, userId, total);
```

**Act:**
```csharp
// Publish same event twice
var eventBus = GetEventBus();
await eventBus.PublishAsync(orderCompletedEvent);
await eventBus.PublishAsync(orderCompletedEvent); // Duplicate

// Wait for both to process
await Task.Delay(TimeSpan.FromSeconds(5));
```

**Assert:**
```csharp
using var dbContext = GetDbContext();
var notifications = await dbContext.Notifications
    .Where(n => n.OrderId == orderId && n.Type == NotificationType.OrderCompleted)
    .ToListAsync();

// Should only have 1 notification despite 2 events
notifications.Should().HaveCount(1);
```

**Why this matters:**
- Verifies idempotency logic in OrderCompletedConsumer.cs:32-35
- Ensures message retries don't create duplicate notifications
- Tests MassTransit message handling reliability

---

### Test #5: Order Expiration Background Job
**Priority:** 🔴 Critical
**File:** `CoderamaOpsAI.IntegrationTests/Worker/OrderExpirationIntegrationTests.cs`
**Estimated Complexity:** High

#### Test Case: `Given_ProcessingOrderOlderThan10Min_When_JobRuns_Then_OrderMarkedExpired`

**Purpose:** Verify OrderExpirationJob correctly expires stale Processing orders

**Arrange:**
```csharp
// Login and create order
var token = await GetAuthToken();
ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

using var dbContext = GetDbContext();
var testUser = await dbContext.Users.FirstAsync();
var testProduct = await dbContext.Products.FirstAsync();

// Create order and manually set to Processing with old timestamp
var order = new Order
{
    UserId = testUser.Id,
    ProductId = testProduct.Id,
    Quantity = 1,
    Price = 99.99m,
    Total = 99.99m,
    Status = OrderStatus.Processing,
    CreatedAt = DateTime.UtcNow.AddMinutes(-15), // 15 minutes ago
    UpdatedAt = DateTime.UtcNow.AddMinutes(-11)  // 11 minutes ago (past 10-min threshold)
};

await dbContext.Orders.AddAsync(order);
await dbContext.SaveChangesAsync();

var orderId = order.Id;
```

**Act:**
```csharp
// Wait for OrderExpirationJob to run (configured for 5-second intervals in tests per IntegrationTestBase.cs:103)
// Job waits 5 seconds before first run (OrderExpirationJob.cs:38), then checks every interval
// Total wait: 5s (initial delay) + 5s (first interval) + 2s (buffer) = 12s
await Task.Delay(TimeSpan.FromSeconds(12));
```

**Assert:**
```csharp
// Verify order status changed to Expired
using var checkDbContext = GetDbContext();
var expiredOrder = await checkDbContext.Orders
    .AsNoTracking()
    .FirstOrDefaultAsync(o => o.Id == orderId);

expiredOrder.Should().NotBeNull();
expiredOrder!.Status.Should().Be(OrderStatus.Expired);
expiredOrder.UpdatedAt.Should().BeAfter(order.UpdatedAt);
expiredOrder.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(15));

// Verify OrderExpiredEvent was published by checking for notification
var notification = await checkDbContext.Notifications
    .AsNoTracking()
    .FirstOrDefaultAsync(n => n.OrderId == orderId && n.Type == NotificationType.OrderExpired);

notification.Should().NotBeNull("OrderExpiredConsumer should have created notification");
```

**Why this matters:**
- Tests critical background job: OrderExpirationJob.cs
- Verifies 10-minute expiration threshold (OrderExpirationJob.cs:29)
- Confirms OrderExpiredEvent is published (OrderExpirationJob.cs:80-81)
- Tests OrderExpiredConsumer creates notification
- Validates scheduled job execution in integration environment

---

#### Test Case: `Given_RecentProcessingOrder_When_JobRuns_Then_OrderNotExpired`

**Purpose:** Verify job doesn't expire recent orders (negative test)

**Arrange:**
```csharp
using var dbContext = GetDbContext();
var testUser = await dbContext.Users.FirstAsync();
var testProduct = await dbContext.Products.FirstAsync();

// Create order with recent timestamp
var order = new Order
{
    UserId = testUser.Id,
    ProductId = testProduct.Id,
    Quantity = 1,
    Price = 99.99m,
    Total = 99.99m,
    Status = OrderStatus.Processing,
    CreatedAt = DateTime.UtcNow.AddMinutes(-5), // 5 minutes ago
    UpdatedAt = DateTime.UtcNow.AddMinutes(-5)  // Within 10-min threshold
};

await dbContext.Orders.AddAsync(order);
await dbContext.SaveChangesAsync();

var orderId = order.Id;
var originalUpdatedAt = order.UpdatedAt;
```

**Act:**
```csharp
// Wait for job to run
await Task.Delay(TimeSpan.FromSeconds(12));
```

**Assert:**
```csharp
using var checkDbContext = GetDbContext();
var stillProcessingOrder = await checkDbContext.Orders
    .AsNoTracking()
    .FirstOrDefaultAsync(o => o.Id == orderId);

stillProcessingOrder.Should().NotBeNull();
stillProcessingOrder!.Status.Should().Be(OrderStatus.Processing,
    "Order should still be Processing because it's newer than 10-minute threshold");
stillProcessingOrder.UpdatedAt.Should().Be(originalUpdatedAt,
    "UpdatedAt should not change if order wasn't expired");
```

**Why this matters:**
- Negative test ensures job logic is correct
- Verifies cutoff calculation: `DateTime.UtcNow - _expirationThreshold` (OrderExpirationJob.cs:61)
- Prevents false positives (expiring valid orders)

---

### Test #6: User-Specific Order Filtering
**Priority:** 🟡 Important
**File:** `CoderamaOpsAI.IntegrationTests/Api/OrderFilteringIntegrationTests.cs`
**Estimated Complexity:** Medium

#### Test Case: `Given_MultipleUsers_When_UserRequestsOrders_Then_OnlyTheirOrdersReturned`

**Purpose:** Verify users only see their own orders (critical security requirement)

**Arrange:**
```csharp
// Create two users with orders
using var dbContext = GetDbContext();

var user1 = new User { Name = "User One", Email = "user1@test.com", Password = BCrypt.Net.BCrypt.HashPassword("pass1"), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
var user2 = new User { Name = "User Two", Email = "user2@test.com", Password = BCrypt.Net.BCrypt.HashPassword("pass2"), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
await dbContext.Users.AddRangeAsync(user1, user2);
await dbContext.SaveChangesAsync();

var product = await dbContext.Products.FirstAsync();

// Create 3 orders for user1 and 2 orders for user2
var user1Orders = new[]
{
    new Order { UserId = user1.Id, ProductId = product.Id, Quantity = 1, Price = 10m, Total = 10m, Status = OrderStatus.Completed, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
    new Order { UserId = user1.Id, ProductId = product.Id, Quantity = 2, Price = 20m, Total = 40m, Status = OrderStatus.Processing, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
    new Order { UserId = user1.Id, ProductId = product.Id, Quantity = 3, Price = 30m, Total = 90m, Status = OrderStatus.Pending, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
};

var user2Orders = new[]
{
    new Order { UserId = user2.Id, ProductId = product.Id, Quantity = 5, Price = 50m, Total = 250m, Status = OrderStatus.Completed, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
    new Order { UserId = user2.Id, ProductId = product.Id, Quantity = 10, Price = 100m, Total = 1000m, Status = OrderStatus.Expired, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
};

await dbContext.Orders.AddRangeAsync(user1Orders);
await dbContext.Orders.AddRangeAsync(user2Orders);
await dbContext.SaveChangesAsync();

var user1OrderIds = user1Orders.Select(o => o.Id).ToList();
var user2OrderIds = user2Orders.Select(o => o.Id).ToList();
```

**Act:**
```csharp
// Login as user1
var user1Token = await GetAuthToken("user1@test.com", "pass1");
ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);

var user1Response = await ApiClient.GetAsync("/api/orders");
var user1OrdersList = await user1Response.Content.ReadFromJsonAsync<List<OrderResponse>>();

// Login as user2
var user2Token = await GetAuthToken("user2@test.com", "pass2");
ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2Token);

var user2Response = await ApiClient.GetAsync("/api/orders");
var user2OrdersList = await user2Response.Content.ReadFromJsonAsync<List<OrderResponse>>();
```

**Assert:**
```csharp
// User1 should only see their 3 orders
user1OrdersList.Should().NotBeNull();
user1OrdersList!.Should().HaveCount(3);
user1OrdersList.Select(o => o.Id).Should().BeEquivalentTo(user1OrderIds);
user1OrdersList.Should().AllSatisfy(o => o.UserId.Should().Be(user1.Id));

// User2 should only see their 2 orders
user2OrdersList.Should().NotBeNull();
user2OrdersList!.Should().HaveCount(2);
user2OrdersList.Select(o => o.Id).Should().BeEquivalentTo(user2OrderIds);
user2OrdersList.Should().AllSatisfy(o => o.UserId.Should().Be(user2.Id));

// Ensure no cross-contamination
user1OrdersList.Select(o => o.Id).Should().NotIntersectWith(user2OrderIds);
user2OrdersList.Select(o => o.Id).Should().NotIntersectWith(user1OrderIds);
```

**Why this matters:**
- **CRITICAL SECURITY REQUIREMENT** from INITIAL_FE.md:15 "list order - user can see only his orders and not orders of other users"
- Currently OrdersController.cs:28-54 returns ALL orders without filtering!
- This test will **FAIL** with current implementation
- **Action required:** Backend must add user filtering: `.Where(o => o.UserId == currentUser.Id)`
- Identifies a potential security vulnerability

---

### Test #7: Complete Order Workflow with Status Transitions
**Priority:** 🟡 Important
**File:** `CoderamaOpsAI.IntegrationTests/Integration/CompleteOrderWorkflowTests.cs`
**Estimated Complexity:** High

#### Test Case: `Given_NewOrder_When_WorkflowCompletes_Then_AllStatusTransitionsOccur`

**Purpose:** Comprehensive end-to-end test of full order lifecycle including all status transitions

**Arrange:**
```csharp
// Setup: Login, get user and product
var token = await GetAuthToken();
ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

using var dbContext = GetDbContext();
var testUser = await dbContext.Users.FirstAsync(u => u.Email == "test@example.com");
var testProduct = await dbContext.Products.FirstAsync();

var createOrderRequest = new CreateOrderRequest
{
    UserId = testUser.Id,
    ProductId = testProduct.Id,
    Quantity = 5,
    Price = 199.99m,
    Status = OrderStatus.Pending
};
```

**Act & Assert (Multi-step):**

**Step 1: Create order (Pending status)**
```csharp
var createResponse = await ApiClient.PostAsJsonAsync("/api/orders", createOrderRequest);
createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

var order = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
order!.Status.Should().Be("Pending");
var orderId = order.Id;

// Verify OrderCreatedEvent was published (check logs or database effects)
```

**Step 2: Wait for OrderCreatedConsumer to process (Pending → Processing/Completed)**
```csharp
// Consumer has 5-second delay (per OrderCreatedConsumer implementation)
await Task.Delay(TimeSpan.FromSeconds(7));

using var dbContext2 = GetDbContext();
var processingOrder = await dbContext2.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
processingOrder.Status.Should().BeOneOf(OrderStatus.Processing, OrderStatus.Completed);

var statusAfterCreation = processingOrder.Status;
```

**Step 3: If Processing, wait for potential completion or expiration**
```csharp
if (statusAfterCreation == OrderStatus.Processing)
{
    // For expiration test: Update order to be 11 minutes old
    using var dbContext3 = GetDbContext();
    var orderToAge = await dbContext3.Orders.FirstAsync(o => o.Id == orderId);
    orderToAge.UpdatedAt = DateTime.UtcNow.AddMinutes(-11);
    await dbContext3.SaveChangesAsync();

    // Wait for OrderExpirationJob (runs every 5 seconds)
    await Task.Delay(TimeSpan.FromSeconds(12));

    using var dbContext4 = GetDbContext();
    var finalOrder = await dbContext4.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
    finalOrder.Status.Should().Be(OrderStatus.Expired);
}
```

**Step 4: Verify notifications were created**
```csharp
using var dbContext5 = GetDbContext();
var notifications = await dbContext5.Notifications
    .Where(n => n.OrderId == orderId)
    .ToListAsync();

// Should have at least 1 notification (OrderCompleted or OrderExpired)
notifications.Should().NotBeEmpty();

if (statusAfterCreation == OrderStatus.Completed)
{
    notifications.Should().Contain(n => n.Type == NotificationType.OrderCompleted);
}
else
{
    notifications.Should().Contain(n => n.Type == NotificationType.OrderExpired);
}
```

**Step 5: Verify via API that order reflects final state**
```csharp
var finalResponse = await ApiClient.GetAsync($"/api/orders/{orderId}");
var finalOrderResponse = await finalResponse.Content.ReadFromJsonAsync<OrderResponse>();

finalOrderResponse.Should().NotBeNull();
finalOrderResponse!.UpdatedAt.Should().BeAfter(order.CreatedAt);
```

**Why this matters:**
- Tests the complete happy path and expiration path
- Verifies all system components work together: API → RabbitMQ → Worker → Database
- Confirms event-driven architecture is functioning
- Validates timing and delays are correct
- Comprehensive regression test for entire order processing system

---

### Test #8: Concurrent Order Creation (Race Conditions)
**Priority:** 🟡 Important
**File:** `CoderamaOpsAI.IntegrationTests/Integration/ConcurrencyTests.cs`
**Estimated Complexity:** Medium-High

#### Test Case: `Given_MultipleSimultaneousOrders_When_Created_Then_AllPersistCorrectly`

**Purpose:** Verify system handles concurrent requests without data corruption

**Arrange:**
```csharp
var token = await GetAuthToken();

using var dbContext = GetDbContext();
var testUser = await dbContext.Users.FirstAsync();
var testProduct = await dbContext.Products.FirstAsync();

// Create 10 order requests
var requests = Enumerable.Range(1, 10).Select(i => new CreateOrderRequest
{
    UserId = testUser.Id,
    ProductId = testProduct.Id,
    Quantity = i,
    Price = i * 10m,
    Status = OrderStatus.Pending
}).ToList();
```

**Act:**
```csharp
// Create 10 HTTP clients (simulate 10 concurrent users)
var clients = Enumerable.Range(0, 10)
    .Select(_ =>
    {
        var client = _apiFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    })
    .ToList();

// Send all requests simultaneously
var createTasks = requests.Select((req, index) =>
    clients[index].PostAsJsonAsync("/api/orders", req)
).ToList();

var responses = await Task.WhenAll(createTasks);
```

**Assert:**
```csharp
// All requests should succeed
responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.Created));

// Parse all order IDs
var orderIds = new List<int>();
foreach (var response in responses)
{
    var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
    orderIds.Add(order!.Id);
}

// All IDs should be unique
orderIds.Should().OnlyHaveUniqueItems();
orderIds.Should().HaveCount(10);

// Verify all orders exist in database
using var checkDbContext = GetDbContext();
var dbOrders = await checkDbContext.Orders
    .Where(o => orderIds.Contains(o.Id))
    .ToListAsync();

dbOrders.Should().HaveCount(10);

// Verify quantities and totals are correct (no data corruption)
for (int i = 0; i < 10; i++)
{
    var request = requests[i];
    var dbOrder = dbOrders.First(o => o.Quantity == request.Quantity);

    dbOrder.Price.Should().Be(request.Price);
    dbOrder.Total.Should().Be(request.Quantity * request.Price);
}
```

**Why this matters:**
- Tests database transaction isolation
- Verifies EF Core concurrency handling
- Confirms MassTransit can handle burst of messages
- Identifies potential race conditions in order creation
- Performance test for system under load

---

### Test #9: Order Total Calculation Verification
**Priority:** 🟡 Important
**File:** `CoderamaOpsAI.IntegrationTests/Api/OrderBusinessLogicTests.cs`
**Estimated Complexity:** Low

#### Test Case: `Given_OrderWithQuantityAndPrice_When_Created_Then_TotalCalculatedCorrectly`

**Purpose:** Verify server-side total calculation (security: prevent client tampering)

**Arrange:**
```csharp
var token = await GetAuthToken();
ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

using var dbContext = GetDbContext();
var testUser = await dbContext.Users.FirstAsync();
var testProduct = await dbContext.Products.FirstAsync();

var testCases = new[]
{
    new { Quantity = 1, Price = 99.99m, ExpectedTotal = 99.99m },
    new { Quantity = 5, Price = 49.99m, ExpectedTotal = 249.95m },
    new { Quantity = 100, Price = 1.50m, ExpectedTotal = 150.00m },
    new { Quantity = 7, Price = 123.45m, ExpectedTotal = 864.15m }
};
```

**Act & Assert:**
```csharp
foreach (var testCase in testCases)
{
    var request = new CreateOrderRequest
    {
        UserId = testUser.Id,
        ProductId = testProduct.Id,
        Quantity = testCase.Quantity,
        Price = testCase.Price,
        Status = OrderStatus.Pending
    };

    var response = await ApiClient.PostAsJsonAsync("/api/orders", request);
    var order = await response.Content.ReadFromJsonAsync<OrderResponse>();

    // Verify API response
    order!.Total.Should().Be(testCase.ExpectedTotal);

    // Verify database
    using var checkDb = GetDbContext();
    var dbOrder = await checkDb.Orders.FindAsync(order.Id);
    dbOrder!.Total.Should().Be(testCase.ExpectedTotal);
}
```

**Why this matters:**
- Verifies OrdersController.cs:110 calculation: `var total = request.Quantity * request.Price;`
- Security: Ensures client cannot manipulate total
- Precision: Tests decimal arithmetic edge cases
- Referenced in docs/integration-test-analysis.md gap: "order total calculation verification"

---

### Test #10: Error Handling - 404 Not Found Scenarios
**Priority:** 🟡 Important
**File:** `CoderamaOpsAI.IntegrationTests/Api/ErrorHandlingTests.cs`
**Estimated Complexity:** Low

#### Test Case: `Given_NonExistentResourceId_When_GetById_Then_Returns404`

**Purpose:** Verify proper 404 handling across all endpoints

**Arrange:**
```csharp
var token = await GetAuthToken();
ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

var nonExistentId = 999999;
```

**Act & Assert:**
```csharp
// Test products endpoint
var productResponse = await ApiClient.GetAsync($"/api/products/{nonExistentId}");
productResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
var productError = await productResponse.Content.ReadFromJsonAsync<ErrorResponse>();
productError!.Message.Should().Contain("Product not found");

// Test orders endpoint
var orderResponse = await ApiClient.GetAsync($"/api/orders/{nonExistentId}");
orderResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
var orderError = await orderResponse.Content.ReadFromJsonAsync<ErrorResponse>();
orderError!.Message.Should().Contain("Order not found");

// Test users endpoint
var userResponse = await ApiClient.GetAsync($"/api/users/{nonExistentId}");
userResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
var userError = await userResponse.Content.ReadFromJsonAsync<ErrorResponse>();
userError!.Message.Should().Contain("User not found");
```

**Why this matters:**
- Ensures consistent error responses across API
- Verifies global exception middleware is working (CLAUDE.md:148)
- Confirms problem details format (RFC 7807)
- Important for frontend error handling

---

## Implementation Priority & Roadmap

### Phase 1: Critical Security & Core Functionality (Implement First)
1. ✅ **Test #1** - Authentication Flow (valid/invalid credentials)
2. ✅ **Test #2** - Authorization (missing/expired tokens)
3. ✅ **Test #6** - User Order Filtering ⚠️ **Will likely fail - requires backend fix**

### Phase 2: CRUD Operations (Required for Frontend)
4. ✅ **Test #3** - Products CRUD End-to-End
5. ✅ **Test #9** - Order Total Calculation

### Phase 3: Event-Driven System Verification
6. ✅ **Test #4** - Notification System
7. ✅ **Test #5** - Order Expiration Job

### Phase 4: Advanced Scenarios
8. ✅ **Test #7** - Complete Workflow
9. ✅ **Test #8** - Concurrent Operations
10. ✅ **Test #10** - Error Handling

---

## Testing Infrastructure Recommendations

### Test Data Management
- **Current:** Migrations seed initial data
- **Recommendation:** Add test data builder classes for consistent test setup
  ```csharp
  public class TestDataBuilder
  {
      public static User CreateTestUser(string email = "test@example.com") { ... }
      public static Product CreateTestProduct(string name = "Test Product") { ... }
  }
  ```

### Test Cleanup
- **Current:** Database persists between tests
- **Recommendation:** Implement `IAsyncLifetime` per test class for cleanup
- **Alternative:** Use database transactions and rollback after each test

### Test Organization
- Create separate test classes by feature area:
  - `AuthenticationIntegrationTests.cs`
  - `ProductsIntegrationTests.cs`
  - `OrdersIntegrationTests.cs`
  - `NotificationIntegrationTests.cs`
  - `BackgroundJobsIntegrationTests.cs`

### Performance Considerations
- Current tests take ~7-20 seconds each due to worker delays
- **Recommendation:** Configure faster intervals for test environment (already done: `OrderExpiration:IntervalSeconds = 5`)
- Consider using `ITestOutputHelper` for detailed timing logs

### CI/CD Integration
- Tests use Testcontainers (require Docker)
- Ensure CI pipeline has Docker available
- Consider parallel test execution (xUnit supports this)

---

## Known Issues & Action Items

### 🚨 Critical Issue Identified
**Test #6 will fail with current implementation:**
- `OrdersController.GetAll()` returns ALL orders without filtering by user
- **Required fix:** Add `.Where(o => o.UserId == currentUser.Id)` in OrdersController.cs:33
- **Security impact:** Users can currently see other users' orders!

### Recommended Code Changes

#### OrdersController.cs - Add User Filtering
```csharp
// Current (line 33):
var orders = await _dbContext.Orders
    .Include(o => o.User)
    .Include(o => o.Product)
    .AsNoTracking()
    .ToListAsync(cancellationToken);

// Recommended:
var currentUserId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
var orders = await _dbContext.Orders
    .Include(o => o.User)
    .Include(o => o.Product)
    .Where(o => o.UserId == currentUserId) // Add this line
    .AsNoTracking()
    .ToListAsync(cancellationToken);
```

---

## Summary Statistics

| Metric | Current | After Tests | Target |
|--------|---------|-------------|--------|
| Integration Tests | 2 | 12 | 10+ |
| API Coverage | 10% | 80% | 70% |
| Worker Coverage | 50% | 100% | 90% |
| Auth Coverage | 0% | 100% | 100% |
| CRUD Coverage | 0% | 60% | 60% |

---

## Next Steps

1. **Implement Test #6 first** to identify the user filtering security issue
2. **Fix OrdersController** to filter orders by current user
3. **Implement Tests #1-5** (Critical priority)
4. **Add test data builders** for cleaner test code
5. **Implement Tests #7-10** (Important priority)
6. **Add test documentation** to README.md
7. **Configure CI/CD** to run integration tests on every commit

---

## Additional Resources

- **Current Test Base:** `CoderamaOpsAI.IntegrationTests/Infrastructure/IntegrationTestBase.cs`
- **Example Test:** `CoderamaOpsAI.IntegrationTests/OrderProcessingIntegrationTests.cs`
- **Unit Tests Reference:** `CoderamaOpsAI.UnitTests/`
- **Testcontainers Docs:** https://testcontainers.com/guides/getting-started-with-testcontainers-for-dotnet/
- **MassTransit Testing:** https://masstransit.io/documentation/concepts/testing

---

**Document Version:** 1.0
**Last Updated:** 2025-11-26
**Author:** Integration Test Analysis Tool
