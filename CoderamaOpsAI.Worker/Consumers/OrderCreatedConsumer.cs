using CoderamaOpsAI.Common.Events;
using CoderamaOpsAI.Common.Interfaces;
using CoderamaOpsAI.Dal;
using CoderamaOpsAI.Dal.Entities;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CoderamaOpsAI.Worker.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly AppDbContext _dbContext;
    private readonly IPaymentSimulator _paymentSimulator;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(
        AppDbContext dbContext,
        IPaymentSimulator paymentSimulator,
        IEventBus eventBus,
        ILogger<OrderCreatedConsumer> logger)
    {
        _dbContext = dbContext;
        _paymentSimulator = paymentSimulator;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var orderId = context.Message.OrderId;
        _logger.LogInformation("Processing OrderCreated for OrderId={OrderId}", orderId);

        var order = await _dbContext.Orders.FindAsync(orderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found, skipping", orderId);
            return;
        }

        // Idempotency: skip if already processed
        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogInformation("Order {OrderId} already processed (status={Status})",
                orderId, order.Status);
            return;
        }

        // Update to Processing
        order.Status = OrderStatus.Processing;
        order.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Order {OrderId} status updated to Processing", orderId);

        // Simulate payment (5 seconds)
        await Task.Delay(TimeSpan.FromSeconds(5), context.CancellationToken);

        // 50% chance to complete
        if (_paymentSimulator.ShouldCompletePayment())
        {
            order.Status = OrderStatus.Completed;
            order.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            await _eventBus.PublishAsync(new OrderCompletedEvent(
                order.Id,
                order.UserId,
                order.Total
            ), context.CancellationToken);

            _logger.LogInformation("Order {OrderId} completed and event published", orderId);
        }
        else
        {
            _logger.LogInformation("Order {OrderId} remains in Processing (payment pending)", orderId);
        }
    }
}
