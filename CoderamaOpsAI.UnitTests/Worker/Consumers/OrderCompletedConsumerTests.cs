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
