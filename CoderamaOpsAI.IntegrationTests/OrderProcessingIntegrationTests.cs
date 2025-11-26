using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CoderamaOpsAI.Api.Models;
using CoderamaOpsAI.Dal.Entities;
using CoderamaOpsAI.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CoderamaOpsAI.IntegrationTests;

public class OrderProcessingIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Given_OrderCreated_When_WorkerProcesses_Then_StatusChangesFromPending()
    {
        // Arrange - Login to get JWT token
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Test123!"
        };

        var loginResponse = await ApiClient.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult.Should().NotBeNull();
        loginResult!.Token.Should().NotBeNullOrEmpty();

        // Set authorization header for subsequent requests
        ApiClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult.Token);

        // Arrange - Get test user and product IDs from database
        using var dbContext = GetDbContext();
        var testUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "test@example.com");
        testUser.Should().NotBeNull();

        var testProduct = await dbContext.Products.FirstOrDefaultAsync();
        testProduct.Should().NotBeNull();

        // Arrange - Create order request
        var createOrderRequest = new CreateOrderRequest
        {
            UserId = testUser!.Id,
            ProductId = testProduct!.Id,
            Quantity = 2,
            Price = 99.99m,
            Status = OrderStatus.Pending
        };

        // Act - Create order via API
        var createResponse = await ApiClient.PostAsJsonAsync("/api/orders", createOrderRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var orderResponse = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
        orderResponse.Should().NotBeNull();
        orderResponse!.Status.Should().Be("Pending");
        orderResponse.Id.Should().BeGreaterThan(0);

        var orderId = orderResponse.Id;

        // Assert - Wait for worker to process the order (OrderCreatedConsumer takes ~5 seconds)
        // We'll check multiple times to see if status changed from Pending
        var statusChanged = false;
        var finalStatus = "Pending";
        var maxWaitTime = TimeSpan.FromSeconds(20);
        var checkInterval = TimeSpan.FromSeconds(2);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < maxWaitTime && !statusChanged)
        {
            await Task.Delay(checkInterval);

            // Query database directly to check order status
            using var checkDbContext = GetDbContext();
            var order = await checkDbContext.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);

            order.Should().NotBeNull();
            finalStatus = order!.Status.ToString();

            if (order.Status != OrderStatus.Pending)
            {
                statusChanged = true;
                break;
            }
        }

        // Assert - Order status should have changed from Pending to Processing or Completed
        statusChanged.Should().BeTrue(
            "Worker should have processed the order and changed status from Pending within {0} seconds",
            maxWaitTime.TotalSeconds);

        finalStatus.Should().BeOneOf("Processing", "Completed",
            "Worker should change order status to either Processing or Completed");

        // Additional verification - Check via API
        var getResponse = await ApiClient.GetAsync($"/api/orders/{orderId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedOrderResponse = await getResponse.Content.ReadFromJsonAsync<OrderResponse>();
        updatedOrderResponse.Should().NotBeNull();
        updatedOrderResponse!.Status.Should().NotBe("Pending",
            "Order status should no longer be Pending after worker processing");
        updatedOrderResponse.Status.Should().Be(finalStatus);
    }

    [Fact]
    public async Task Given_OrderCreated_When_WorkerProcesses_Then_OrderCompletedOrProcessing()
    {
        // Arrange - Login
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Test123!"
        };

        var loginResponse = await ApiClient.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        ApiClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.Token);

        // Arrange - Get test data
        using var dbContext = GetDbContext();
        var testUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "test@example.com");
        var testProduct = await dbContext.Products.FirstOrDefaultAsync();

        // Arrange - Create order
        var createOrderRequest = new CreateOrderRequest
        {
            UserId = testUser!.Id,
            ProductId = testProduct!.Id,
            Quantity = 1,
            Price = 49.99m,
            Status = OrderStatus.Pending
        };

        // Act - Create order
        var createResponse = await ApiClient.PostAsJsonAsync("/api/orders", createOrderRequest);
        var orderResponse = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
        var orderId = orderResponse!.Id;

        // Wait for processing (5 seconds delay in consumer + processing time)
        await Task.Delay(TimeSpan.FromSeconds(7));

        // Assert - Check final state
        using var checkDbContext = GetDbContext();
        var processedOrder = await checkDbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        processedOrder.Should().NotBeNull();
        processedOrder!.Status.Should().NotBe(OrderStatus.Pending,
            "Order should be processed by worker");

        // Worker has 50% chance to complete or leave in Processing
        processedOrder.Status.Should().BeOneOf(OrderStatus.Processing, OrderStatus.Completed);

        // Verify UpdatedAt timestamp changed
        processedOrder.UpdatedAt.Should().BeAfter(processedOrder.CreatedAt,
            "UpdatedAt should be updated when worker processes the order");
    }
}
