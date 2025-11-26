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
