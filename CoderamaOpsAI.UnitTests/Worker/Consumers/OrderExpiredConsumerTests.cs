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

public class OrderExpiredConsumerTests : DatabaseTestBase
{
    private readonly ILogger<OrderExpiredConsumer> _logger;
    private readonly OrderExpiredConsumer _consumer;

    public OrderExpiredConsumerTests()
    {
        _logger = Substitute.For<ILogger<OrderExpiredConsumer>>();
        _consumer = new OrderExpiredConsumer(DbContext, _logger);
    }

    [Fact]
    public async Task Given_OrderExpiredEvent_When_Consume_Then_CreatesNotification()
    {
        // Arrange
        var @event = new OrderExpiredEvent(1, 10);
        var context = Substitute.For<ConsumeContext<OrderExpiredEvent>>();
        context.Message.Returns(@event);

        // Act
        await _consumer.Consume(context);

        // Assert
        var notification = await DbContext.Notifications
            .FirstOrDefaultAsync(n => n.OrderId == 1 && n.Type == NotificationType.OrderExpired);

        notification.Should().NotBeNull();
        notification!.Message.Should().Contain("Order #1 expired");
        notification.Metadata.Should().Contain("UserId");
    }

    [Fact]
    public async Task Given_DuplicateEvent_When_Consume_Then_Idempotent()
    {
        // Arrange - notification already exists
        var existing = new Notification
        {
            OrderId = 2,
            Type = NotificationType.OrderExpired,
            Message = "Already exists",
            CreatedAt = DateTime.UtcNow
        };
        await DbContext.Notifications.AddAsync(existing);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        var @event = new OrderExpiredEvent(2, 10);
        var context = Substitute.For<ConsumeContext<OrderExpiredEvent>>();
        context.Message.Returns(@event);

        // Act
        await _consumer.Consume(context);

        // Assert - should not create duplicate
        var count = await DbContext.Notifications
            .CountAsync(n => n.OrderId == 2 && n.Type == NotificationType.OrderExpired);
        count.Should().Be(1);
    }
}
