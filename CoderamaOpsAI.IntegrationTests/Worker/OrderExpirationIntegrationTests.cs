using System.Net.Http.Json;
using CoderamaOpsAI.Api.Models;
using CoderamaOpsAI.Dal.Entities;
using CoderamaOpsAI.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CoderamaOpsAI.IntegrationTests.Worker;

public class OrderExpirationIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Given_ProcessingOrderOlderThan10Min_When_JobRuns_Then_OrderMarkedExpired()
    {
        // Arrange - Authenticate and create order
        var token = await GetAuthTokenAsync("test@example.com", "Test123!");
        SetAuthorizationHeader(token);

        using var dbContext = GetDbContext();
        var testUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "test@example.com");
        var testProduct = await dbContext.Products.FirstOrDefaultAsync();

        var createOrderRequest = new CreateOrderRequest
        {
            UserId = testUser!.Id,
            ProductId = testProduct!.Id,
            Quantity = 1,
            Price = 99.99m,
            Status = OrderStatus.Processing
        };

        var createResponse = await ApiClient.PostAsJsonAsync("/api/orders", createOrderRequest);
        var orderResponse = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
        var orderId = orderResponse!.Id;

        // Arrange - Manually set UpdatedAt to 11 minutes ago (older than 10-minute threshold)
        using var manipulateDbContext = GetDbContext();
        var order = await manipulateDbContext.Orders.FindAsync(orderId);
        order.Should().NotBeNull();

        var elevenMinutesAgo = DateTime.UtcNow.AddMinutes(-11);
        order!.UpdatedAt = elevenMinutesAgo;
        await manipulateDbContext.SaveChangesAsync();

        // Act - Wait for OrderExpirationJob to run
        // Job has 5-second initial delay + 5-second interval, wait 12 seconds total
        await Task.Delay(TimeSpan.FromSeconds(12));

        // Assert - Verify order status changed to Expired
        using var verifyDbContext = GetDbContext();
        var expiredOrder = await verifyDbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        expiredOrder.Should().NotBeNull();
        expiredOrder!.Status.Should().Be(OrderStatus.Expired,
            "Order older than 10 minutes in Processing status should be marked as Expired");
        expiredOrder.UpdatedAt.Should().BeAfter(elevenMinutesAgo,
            "UpdatedAt should be updated when order is marked expired");
        expiredOrder.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(15));

        // Assert - Verify notification created
        var notification = await verifyDbContext.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.OrderId == orderId &&
                                     n.Type == NotificationType.OrderExpired);

        notification.Should().NotBeNull("OrderExpired notification should be created");
        notification!.OrderId.Should().Be(orderId);
        notification.Type.Should().Be(NotificationType.OrderExpired);
    }

    [Fact]
    public async Task Given_RecentProcessingOrder_When_JobRuns_Then_OrderNotExpired()
    {
        // Arrange - Authenticate and create order
        var token = await GetAuthTokenAsync("test@example.com", "Test123!");
        SetAuthorizationHeader(token);

        using var dbContext = GetDbContext();
        var testUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "test@example.com");
        var testProduct = await dbContext.Products.FirstOrDefaultAsync();

        var createOrderRequest = new CreateOrderRequest
        {
            UserId = testUser!.Id,
            ProductId = testProduct!.Id,
            Quantity = 2,
            Price = 49.99m,
            Status = OrderStatus.Processing
        };

        var createResponse = await ApiClient.PostAsJsonAsync("/api/orders", createOrderRequest);
        var orderResponse = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
        var orderId = orderResponse!.Id;

        // Note: UpdatedAt is recent (just created), within 10-minute threshold

        // Act - Wait for OrderExpirationJob to run
        await Task.Delay(TimeSpan.FromSeconds(12));

        // Assert - Verify order status is STILL Processing (not expired)
        using var verifyDbContext = GetDbContext();
        var order = await verifyDbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        order.Should().NotBeNull();
        order!.Status.Should().Be(OrderStatus.Processing,
            "Recent Processing orders (< 10 minutes) should NOT be expired");

        // Assert - Verify NO notification created
        var notification = await verifyDbContext.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.OrderId == orderId &&
                                     n.Type == NotificationType.OrderExpired);

        notification.Should().BeNull("No OrderExpired notification should exist for recent orders");
    }
}
