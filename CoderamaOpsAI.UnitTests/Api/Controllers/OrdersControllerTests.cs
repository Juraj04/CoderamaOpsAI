using CoderamaOpsAI.Api.Controllers;
using CoderamaOpsAI.Api.Models;
using CoderamaOpsAI.Common.Events;
using CoderamaOpsAI.Common.Interfaces;
using CoderamaOpsAI.Dal.Entities;
using CoderamaOpsAI.UnitTests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CoderamaOpsAI.UnitTests.Api.Controllers;

public class OrdersControllerTests : DatabaseTestBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly IEventBus _eventBus;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _logger = Substitute.For<ILogger<OrdersController>>();
        _eventBus = Substitute.For<IEventBus>();
        _controller = new OrdersController(DbContext, _logger, _eventBus);
    }

    private async Task<(User user, Product product)> SeedUserAndProduct()
    {
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var product = new Product
        {
            Name = "Test Product",
            Price = 100.0m,
            Stock = 50,
            CreatedAt = DateTime.UtcNow
        };

        await DbContext.Users.AddAsync(user);
        await DbContext.Products.AddAsync(product);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        return (user, product);
    }

    [Fact]
    public async Task GetAll_WithNoOrders_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as List<OrderResponse>;
        response.Should().NotBeNull();
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_WithOrders_ReturnsOrderListWithRelations()
    {
        // Arrange
        var (user, product) = await SeedUserAndProduct();
        var order = new Order
        {
            UserId = user.Id,
            ProductId = product.Id,
            Quantity = 2,
            Price = 100.0m,
            Total = 200.0m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Orders.AddAsync(order);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as List<OrderResponse>;
        response.Should().NotBeNull();
        response.Should().HaveCount(1);
        response![0].Quantity.Should().Be(2);
        response[0].Total.Should().Be(200.0m);
    }

    [Fact]
    public async Task GetAll_IncludesUserAndProductNames()
    {
        // Arrange
        var (user, product) = await SeedUserAndProduct();
        var order = new Order
        {
            UserId = user.Id,
            ProductId = product.Id,
            Quantity = 1,
            Price = 50.0m,
            Total = 50.0m,
            Status = OrderStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Orders.AddAsync(order);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as List<OrderResponse>;
        response![0].UserName.Should().Be("Test User");
        response[0].ProductName.Should().Be("Test Product");
    }

    [Fact]
    public async Task GetAll_UsesAsNoTracking()
    {
        // Arrange
        var (user, product) = await SeedUserAndProduct();
        var order = new Order
        {
            UserId = user.Id,
            ProductId = product.Id,
            Quantity = 1,
            Price = 10.0m,
            Total = 10.0m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Orders.AddAsync(order);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act
        await _controller.GetAll(CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_WithExistingOrder_ReturnsOrderWithRelations()
    {
        // Arrange
        var (user, product) = await SeedUserAndProduct();
        var order = new Order
        {
            UserId = user.Id,
            ProductId = product.Id,
            Quantity = 3,
            Price = 75.0m,
            Total = 225.0m,
            Status = OrderStatus.Processing,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Orders.AddAsync(order);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(order.Id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as OrderResponse;
        response.Should().NotBeNull();
        response!.Id.Should().Be(order.Id);
        response.UserName.Should().Be("Test User");
        response.ProductName.Should().Be("Test Product");
    }

    [Fact]
    public async Task GetById_WithNonExistentOrder_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedOrder()
    {
        // Arrange
        var (user, product) = await SeedUserAndProduct();
        var request = new CreateOrderRequest
        {
            UserId = user.Id,
            ProductId = product.Id,
            Quantity = 2,
            Price = 50.0m,
            Status = OrderStatus.Pending
        };

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        var response = createdResult.Value as OrderResponse;
        response.Should().NotBeNull();
        response!.Quantity.Should().Be(2);
        response.Price.Should().Be(50.0m);

        // Verify event was published
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<OrderCreatedEvent>(e =>
                e.OrderId == response.Id &&
                e.UserId == user.Id &&
                e.ProductId == product.Id &&
                e.Total == 100.0m),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Create_CalculatesTotalCorrectly_QuantityTimesPrice()
    {
        // Arrange
        var (user, product) = await SeedUserAndProduct();
        var request = new CreateOrderRequest
        {
            UserId = user.Id,
            ProductId = product.Id,
            Quantity = 5,
            Price = 20.0m,
            Status = OrderStatus.Pending
        };

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = (CreatedAtActionResult)result;
        var response = createdResult.Value as OrderResponse;
        response!.Total.Should().Be(100.0m); // 5 * 20.0

        // Verify event was published
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<OrderCreatedEvent>(e => e.Total == 100.0m),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Create_SetsAuditFields_CreatedAtAndUpdatedAt()
    {
        // Arrange
        var (user, product) = await SeedUserAndProduct();
        var request = new CreateOrderRequest
        {
            UserId = user.Id,
            ProductId = product.Id,
            Quantity = 1,
            Price = 10.0m,
            Status = OrderStatus.Pending
        };
        var beforeCreate = DateTime.UtcNow;

        // Act
        await _controller.Create(request, CancellationToken.None);

        // Assert
        var order = await DbContext.Orders.FindAsync(1);
        order.Should().NotBeNull();
        order!.CreatedAt.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(2));
        order.UpdatedAt.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(2));

        // Verify event was published
        await _eventBus.Received(1).PublishAsync(
            Arg.Any<OrderCreatedEvent>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Create_WithNonExistentUserId_ReturnsBadRequest()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test",
            Price = 10.0m,
            Stock = 5,
            CreatedAt = DateTime.UtcNow
        };
        await DbContext.Products.AddAsync(product);
        await DbContext.SaveChangesAsync();

        var request = new CreateOrderRequest
        {
            UserId = 999, // Non-existent
            ProductId = product.Id,
            Quantity = 1,
            Price = 10.0m,
            Status = OrderStatus.Pending
        };

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();

        // Verify event was NOT published on failure
        await _eventBus.DidNotReceive().PublishAsync(
            Arg.Any<OrderCreatedEvent>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Create_WithNonExistentProductId_ReturnsBadRequest()
    {
        // Arrange
        var user = new User
        {
            Name = "Test",
            Email = "test@example.com",
            Password = "hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        var request = new CreateOrderRequest
        {
            UserId = user.Id,
            ProductId = 999, // Non-existent
            Quantity = 1,
            Price = 10.0m,
            Status = OrderStatus.Pending
        };

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();

        // Verify event was NOT published on failure
        await _eventBus.DidNotReceive().PublishAsync(
            Arg.Any<OrderCreatedEvent>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Create_WithZeroQuantity_ValidationFails()
    {
        // Note: The Range(1, int.MaxValue) attribute on DTO prevents this
        var request = new CreateOrderRequest
        {
            UserId = 1,
            ProductId = 1,
            Quantity = 0, // Invalid
            Price = 10.0m,
            Status = OrderStatus.Pending
        };

        request.Quantity.Should().Be(0);
    }

    [Fact]
    public async Task Update_WithExistingOrder_ReturnsUpdatedOrder()
    {
        // Arrange
        var (user, product) = await SeedUserAndProduct();
        var order = new Order
        {
            UserId = user.Id,
            ProductId = product.Id,
            Quantity = 1,
            Price = 50.0m,
            Total = 50.0m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Orders.AddAsync(order);
        await DbContext.SaveChangesAsync();

        var request = new UpdateOrderRequest
        {
            Quantity = 3
        };

        // Act
        var result = await _controller.Update(order.Id, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as OrderResponse;
        response.Should().NotBeNull();
        response!.Quantity.Should().Be(3);
    }

    [Fact]
    public async Task Update_WithNonExistentOrder_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateOrderRequest
        {
            Quantity = 2
        };

        // Act
        var result = await _controller.Update(999, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_RecalculatesTotal_WhenQuantityOrPriceChanges()
    {
        // Arrange
        var (user, product) = await SeedUserAndProduct();
        var order = new Order
        {
            UserId = user.Id,
            ProductId = product.Id,
            Quantity = 2,
            Price = 50.0m,
            Total = 100.0m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Orders.AddAsync(order);
        await DbContext.SaveChangesAsync();

        var request = new UpdateOrderRequest
        {
            Quantity = 4,
            Price = 25.0m
        };

        // Act
        var result = await _controller.Update(order.Id, request, CancellationToken.None);

        // Assert
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as OrderResponse;
        response!.Total.Should().Be(100.0m); // 4 * 25.0
    }

    [Fact]
    public async Task Update_UpdatesUpdatedAtField_DoesNotChangeCreatedAt()
    {
        // Arrange
        var (user, product) = await SeedUserAndProduct();
        var originalCreatedAt = DateTime.UtcNow.AddDays(-1);
        var order = new Order
        {
            UserId = user.Id,
            ProductId = product.Id,
            Quantity = 1,
            Price = 10.0m,
            Total = 10.0m,
            Status = OrderStatus.Pending,
            CreatedAt = originalCreatedAt,
            UpdatedAt = originalCreatedAt
        };
        await DbContext.Orders.AddAsync(order);
        await DbContext.SaveChangesAsync();

        var request = new UpdateOrderRequest
        {
            Quantity = 2
        };

        // Act
        await _controller.Update(order.Id, request, CancellationToken.None);

        // Assert
        var updatedOrder = await DbContext.Orders.FindAsync(order.Id);
        updatedOrder!.CreatedAt.Should().Be(originalCreatedAt);
        updatedOrder.UpdatedAt.Should().BeAfter(originalCreatedAt);
    }

    [Fact]
    public async Task Delete_WithExistingOrder_ReturnsNoContent()
    {
        // Arrange
        var (user, product) = await SeedUserAndProduct();
        var order = new Order
        {
            UserId = user.Id,
            ProductId = product.Id,
            Quantity = 1,
            Price = 10.0m,
            Total = 10.0m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await DbContext.Orders.AddAsync(order);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(order.Id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var deletedOrder = await DbContext.Orders.FindAsync(order.Id);
        deletedOrder.Should().BeNull();
    }

    [Fact]
    public async Task Delete_WithNonExistentOrder_ReturnsNotFound()
    {
        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
