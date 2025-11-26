using System.Net.Http.Json;
using System.Text.Json;
using CoderamaOpsAI.Api.Models;
using CoderamaOpsAI.Common.Events;
using CoderamaOpsAI.Dal.Entities;
using CoderamaOpsAI.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CoderamaOpsAI.IntegrationTests.Worker;

public class NotificationIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Given_OrderCompleted_When_EventProcessed_Then_NotificationCreated()
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
            Price = 99.99m,
            Status = OrderStatus.Completed
        };

        var createResponse = await ApiClient.PostAsJsonAsync("/api/orders", createOrderRequest);
        var orderResponse = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
        var orderId = orderResponse!.Id;
        var total = orderResponse.Total;

        // Arrange - Prepare event
        var eventBus = GetEventBus();
        var orderCompletedEvent = new OrderCompletedEvent(
            OrderId: orderId,
            UserId: testUser.Id,
            Total: total
        );

        // Act - Publish event
        await eventBus.PublishAsync(orderCompletedEvent, CancellationToken.None);

        // Wait for consumer to process
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Assert - Verify notification created
        using var verifyDbContext = GetDbContext();
        var notification = await verifyDbContext.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.OrderId == orderId &&
                                     n.Type == NotificationType.OrderCompleted);

        notification.Should().NotBeNull("Notification should be created for completed order");
        notification!.OrderId.Should().Be(orderId);
        notification.Type.Should().Be(NotificationType.OrderCompleted);
        notification.Message.Should().Contain(orderId.ToString());
        notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

        // Verify metadata contains UserId and Total
        notification.Metadata.Should().NotBeNullOrEmpty();
        var metadata = JsonSerializer.Deserialize<JsonDocument>(notification.Metadata!);
        metadata.Should().NotBeNull();
        metadata!.RootElement.GetProperty("UserId").GetInt32().Should().Be(testUser.Id);
        metadata.RootElement.GetProperty("Total").GetDecimal().Should().Be(total);
    }

    [Fact]
    public async Task Given_DuplicateOrderCompletedEvent_When_Consumed_Then_OnlyOneNotification()
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
            Price = 49.99m,
            Status = OrderStatus.Completed
        };

        var createResponse = await ApiClient.PostAsJsonAsync("/api/orders", createOrderRequest);
        var orderResponse = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
        var orderId = orderResponse!.Id;
        var total = orderResponse.Total;

        // Arrange - Prepare event
        var eventBus = GetEventBus();
        var orderCompletedEvent = new OrderCompletedEvent(
            OrderId: orderId,
            UserId: testUser.Id,
            Total: total
        );

        // Act - Publish event TWICE (duplicate)
        await eventBus.PublishAsync(orderCompletedEvent, CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(2));

        await eventBus.PublishAsync(orderCompletedEvent, CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert - Verify only ONE notification exists (idempotency)
        using var verifyDbContext = GetDbContext();
        var notifications = await verifyDbContext.Notifications
            .AsNoTracking()
            .Where(n => n.OrderId == orderId && n.Type == NotificationType.OrderCompleted)
            .ToListAsync();

        notifications.Should().HaveCount(1,
            "Consumer should be idempotent and create only one notification even when event is published twice");
    }
}
