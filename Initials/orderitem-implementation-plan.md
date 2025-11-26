# Implementation Plan: Add OrderItem Support for Multiple Products Per Order

## Executive Summary

**Goal**: Transform the order system from single product per order to multiple products per order using a new `OrderItem` entity.

**Design Decisions** (confirmed with user):
- ✅ **Update Strategy**: Full replacement (delete all items, create new)
- ✅ **Notifications**: Order-level only (no item details)
- ✅ **Frontend UX**: Dynamic form with add/remove product rows
- ✅ **Stock Validation**: No stock checking (allow any quantity)

**Estimated Total Effort**: 18-24 hours (3-4 working days)

**Risk Level**: Medium (database schema change, breaking API changes)

---

## Implementation Phases

### Phase 1: Database Layer (3-4 hours)

#### 1.1 Create OrderItem Entity
**File**: `CoderamaOpsAI.Dal/Entities/OrderItem.cs` (NEW)

Create new entity with fields:
- `Id`, `OrderId`, `ProductId`, `Quantity`, `Price`, `Subtotal`, `CreatedAt`, `UpdatedAt`
- Navigation properties: `Order`, `Product`
- Price is snapshot at order time (prevents issues if product prices change)

#### 1.2 Update Order Entity
**File**: `CoderamaOpsAI.Dal/Entities/Order.cs`

**Remove**: `ProductId`, `Quantity`, `Price` properties and `Product` navigation

**Add**: `public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();`

**Keep**: `Total`, `Status`, `UserId`, `CreatedAt`, `UpdatedAt`

#### 1.3 Update Product Entity
**File**: `CoderamaOpsAI.Dal/Entities/Product.cs`

Change `Orders` collection → `OrderItems` collection

#### 1.4 Update AppDbContext
**File**: `CoderamaOpsAI.Dal/AppDbContext.cs`

- Add `DbSet<OrderItem>` property
- Add OrderItem configuration in `OnModelCreating`:
  - Cascade delete when Order deleted
  - Restrict delete of Products with OrderItems
  - Indexes on OrderId and ProductId
  - Decimal precision for Price and Subtotal (18,2)
- Remove Order-Product relationship configuration

#### 1.5 Create Database Migration
**CRITICAL**: Requires manual editing!

```bash
dotnet ef migrations add AddOrderItemsTable --project CoderamaOpsAI.Dal --startup-project CoderamaOpsAI.Api
```

**Must manually edit the migration**:
1. Create OrderItems table
2. **Migrate existing data** from Orders to OrderItems using SQL INSERT:
   ```sql
   INSERT INTO "OrderItems" ("OrderId", "ProductId", "Quantity", "Price", "Subtotal", "CreatedAt", "UpdatedAt")
   SELECT "Id", "ProductId", "Quantity", "Price", ("Quantity" * "Price"), "CreatedAt", "UpdatedAt"
   FROM "Orders" WHERE "ProductId" IS NOT NULL;
   ```
3. Drop old columns (ProductId, Quantity, Price) from Orders

**⚠️ ROLLBACK WARNING**: Down migration can only restore ONE item per order. Multi-item orders will lose data on rollback. **MANDATORY DATABASE BACKUP**.

---

### Phase 2: Backend API Layer (5-6 hours)

#### 2.1 Update DTOs
**File**: `CoderamaOpsAI.Api/Models/OrderDtos.cs`

**Create new DTOs**:
```csharp
public class OrderItemDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public class OrderItemResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Subtotal { get; set; }
}
```

**Update existing DTOs**:
- `CreateOrderRequest`: Replace ProductId/Quantity/Price with `List<OrderItemDto> Items`
- `UpdateOrderRequest`: Replace ProductId?/Quantity?/Price? with `List<OrderItemDto>? Items`
- `OrderResponse`: Replace single product fields with `List<OrderItemResponse> Items`

#### 2.2 Update OrdersController - Create Method
**File**: `CoderamaOpsAI.Api/Controllers/OrdersController.cs` (lines 91-158)

**Key changes**:
1. Validate all ProductIds exist (bulk query)
2. Loop through items:
   - Fetch Product.Price (snapshot)
   - Create OrderItem with calculated Subtotal
3. Calculate Order.Total as sum of all Subtotals
4. Update OrderCreatedEvent: Remove ProductId parameter

**Validation to add**: Check items list is not empty, check for duplicate ProductIds

**No stock validation** per user decision.

#### 2.3 Update OrdersController - GetAll/GetById
**File**: `CoderamaOpsAI.Api/Controllers/OrdersController.cs`

- Change `.Include(o => o.Product)` to `.Include(o => o.OrderItems).ThenInclude(oi => oi.Product)`
- Update mapping to include Items collection in response
- GetAll: lines 28-56
- GetById: lines 58-89

#### 2.4 Update OrdersController - Update Method
**File**: `CoderamaOpsAI.Api/Controllers/OrdersController.cs` (lines 160-241)

**Full replacement strategy**:
1. If `request.Items != null`:
   - Validate all ProductIds exist
   - Remove existing: `_dbContext.OrderItems.RemoveRange(order.OrderItems)`
   - Create new OrderItems from request
   - Recalculate Order.Total
2. Update UpdatedAt timestamp

#### 2.5 OrdersController - Delete Method
**No changes needed** - cascade delete handles OrderItems automatically

---

### Phase 3: Worker Layer (1-2 hours)

#### 3.1 Update OrderCreatedEvent
**File**: `CoderamaOpsAI.Common/Events/OrderCreatedEvent.cs`

Remove `int ProductId` parameter:
```csharp
// BEFORE
public record OrderCreatedEvent(int OrderId, int UserId, int ProductId, decimal Total);

// AFTER
public record OrderCreatedEvent(int OrderId, int UserId, decimal Total);
```

#### 3.2 Update OrdersController Event Publishing
**File**: `CoderamaOpsAI.Api/Controllers/OrdersController.cs` (lines 128-134)

Remove ProductId from event:
```csharp
await _eventBus.PublishAsync(new OrderCreatedEvent(
    order.Id,
    order.UserId,
    order.Total  // ProductId removed
), cancellationToken);
```

#### 3.3 Worker Consumers
**Files**: OrderCreatedConsumer.cs, OrderCompletedConsumer.cs, OrderExpiredConsumer.cs

**Minimal changes**: These consumers only use Order.Status and Order.Total, which still exist.

**Keep notifications order-level** (no item details) per user decision.

---

### Phase 4: Frontend Layer (5-7 hours)

#### 4.1 Update TypeScript Types
**File**: `coderama-ops-frontend/src/types/order.types.ts`

**Add new interfaces**:
```typescript
export interface OrderItem {
  id: number;
  productId: number;
  productName: string;
  quantity: number;
  price: number;
  subtotal: number;
}

export interface OrderItemDto {
  productId: number;
  quantity: number;
}
```

**Update existing**:
- `Order`: Remove productId/productName/quantity/price, add `items: OrderItem[]`
- `CreateOrderRequest`: Remove productId/quantity/price, add `items: OrderItemDto[]`

#### 4.2 Rebuild OrderForm Component
**File**: `coderama-ops-frontend/src/components/Orders/OrderForm.tsx`

**Complete redesign for dynamic rows**:

**State changes**:
- Replace `selectedProduct` + `quantity` with `cartItems: { tempId: string, productId: number, quantity: number }[]`
- Track used ProductIds to prevent duplicates

**UI features**:
- Multiple product rows (map over cartItems)
- Each row: Product dropdown + Quantity input + Remove button + Subtotal display
- "Add Item" button (adds new row)
- Grand total calculated from all items
- Disable product options that are already added
- Remove button disabled if only 1 item

**Form submission**:
- Map cartItems to OrderItemDto[] format
- Send to API

#### 4.3 Update OrderList Component
**File**: `coderama-ops-frontend/src/components/Orders/OrderList.tsx`

**Display strategy**:
- Replace "Product" column with "Items" showing summary
- Collapsed view: Show first product name + badge "+ N more" if multiple
- Add expand/collapse functionality per row
- Expanded view: Show nested table with all items (productName, quantity, price, subtotal)
- Update total column to show sum of all items

---

### Phase 5: Testing (4-5 hours)

#### 5.1 Update Unit Tests
**Files**: `CoderamaOpsAI.UnitTests/Api/Controllers/OrdersControllerTests.cs`

**Major changes**:
- Update all test data to use OrderItems collections
- Update event assertions (no ProductId)
- Add multi-item scenarios

**New tests to add**:
- Create with multiple items (3+ products)
- Create with empty items list (should return 400)
- Create with duplicate ProductIds (should return 400)
- Create with invalid ProductId (should return 400)
- Update replacing all items
- Verify total calculation across multiple items

#### 5.2 Integration Tests
**Files**: `CoderamaOpsAI.IntegrationTests/`

Update end-to-end flows:
- OrderProcessingIntegrationTests: Use multi-item orders
- NotificationIntegrationTests: Verify order-level notifications work
- Test complete workflow: Create multi-item order → Worker processes → Notifications created

---

### Phase 6: Documentation (1 hour)

#### 6.1 Update README
Document breaking API changes:
- New request/response format with items array
- Migration instructions
- Example API calls

#### 6.2 API Documentation
Update Swagger annotations if using XML comments

---

## Deployment Plan

### Pre-Deployment Checklist
1. ✅ **BACKUP DATABASE**: `pg_dump -U postgres coderamaopsai > backup_$(date +%Y%m%d).sql`
2. ✅ Test migration on local/staging environment
3. ✅ Run all tests: `dotnet test`
4. ✅ Build frontend: `npm run build`
5. ✅ Review migration SQL carefully

### Deployment Steps
1. **Stop services**: `docker-compose down`
2. **Apply migration**: `dotnet ef database update --project CoderamaOpsAI.Dal --startup-project CoderamaOpsAI.Api`
3. **Verify migration**: Check OrderItems table has data from existing Orders
4. **Rebuild containers**: `docker-compose up -d --build`
5. **Verify services**: Check API at localhost:5000/swagger
6. **Test manually**: Create multi-item order via UI

### Rollback Plan (Emergency)
```bash
# Stop services
docker-compose down

# Restore database backup
psql -U postgres coderamaopsai < backup_YYYYMMDD.sql

# Revert code to previous commit
git reset --hard HEAD~1

# Restart services
docker-compose up -d
```

**WARNING**: Rolling back loses any orders created with multiple items (only first item preserved).

---

## Risk Assessment

### 🔴 High Risk
**Data Loss on Rollback**: Multi-item orders lose all but first item if rolled back.
- Mitigation: Mandatory backup, test thoroughly on staging

**Breaking API Changes**: Existing clients will break.
- Mitigation: No external clients in this project, coordinate frontend/backend deployment

### 🟡 Medium Risk
**Migration Complexity**: Manual SQL editing required.
- Mitigation: Peer review migration, test on staging

**Frontend Complexity**: Complete OrderForm rewrite.
- Mitigation: Incremental development, test each feature

### 🟢 Low Risk
**Worker Changes**: Minimal impact, consumers already decoupled.
- Mitigation: Standard testing

---

## Critical Files for Implementation

### Database & Entities (Phase 1)
1. `CoderamaOpsAI.Dal/Entities/OrderItem.cs` - **NEW entity**
2. `CoderamaOpsAI.Dal/Entities/Order.cs` - Remove fields, add collection
3. `CoderamaOpsAI.Dal/Entities/Product.cs` - Update navigation property
4. `CoderamaOpsAI.Dal/AppDbContext.cs` - Add OrderItem configuration
5. `CoderamaOpsAI.Dal/Migrations/[timestamp]_AddOrderItemsTable.cs` - **CRITICAL data migration**

### Backend API (Phase 2)
6. `CoderamaOpsAI.Api/Models/OrderDtos.cs` - Redefine all DTOs
7. `CoderamaOpsAI.Api/Controllers/OrdersController.cs` - Major refactor (Create, GetAll, GetById, Update)

### Worker (Phase 3)
8. `CoderamaOpsAI.Common/Events/OrderCreatedEvent.cs` - Remove ProductId parameter

### Frontend (Phase 4)
9. `coderama-ops-frontend/src/types/order.types.ts` - Redefine all types
10. `coderama-ops-frontend/src/components/Orders/OrderForm.tsx` - **Complete redesign**
11. `coderama-ops-frontend/src/components/Orders/OrderList.tsx` - Update display with expand/collapse

### Testing (Phase 5)
12. `CoderamaOpsAI.UnitTests/Api/Controllers/OrdersControllerTests.cs` - Major test updates
13. `CoderamaOpsAI.IntegrationTests/Api/OrderProcessingIntegrationTests.cs` - Update for multi-item orders

---

## Success Criteria

✅ All migrations applied successfully
✅ All tests pass (unit + integration)
✅ Existing single-item orders migrated correctly (verified in DB)
✅ Can create new multi-item orders via API
✅ Can create new multi-item orders via UI
✅ Worker processes multi-item orders correctly
✅ Notifications created for completed/expired orders
✅ Frontend displays multi-item orders with expand/collapse
✅ No breaking errors in production after 24 hours

---

## Estimated Timeline

| Phase | Effort | Parallel Work Possible |
|-------|--------|------------------------|
| Phase 1: Database | 3-4 hours | No (foundation) |
| Phase 2: Backend API | 5-6 hours | No (depends on Phase 1) |
| Phase 3: Worker | 1-2 hours | Yes (parallel with Phase 4) |
| Phase 4: Frontend | 5-7 hours | Yes (parallel with Phase 3) |
| Phase 5: Testing | 4-5 hours | No (depends on all phases) |
| Phase 6: Documentation | 1 hour | Yes (parallel with Phase 5) |
| **Total** | **18-24 hours** | **3-4 working days (solo)** |

With pair programming or 2 developers: **2-3 working days**

---

## Next Steps

1. ✅ Review this plan
2. ✅ Create database backup
3. ✅ Begin Phase 1 (Database Layer)
4. ✅ Test migration on staging
5. ✅ Proceed through phases sequentially
6. ✅ Deploy backend first, then frontend
