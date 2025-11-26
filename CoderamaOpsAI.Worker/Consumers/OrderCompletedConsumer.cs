using System.Text.Json;
using CoderamaOpsAI.Common.Events;
using CoderamaOpsAI.Dal;
using CoderamaOpsAI.Dal.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoderamaOpsAI.Worker.Consumers;

public class OrderCompletedConsumer : IConsumer<OrderCompletedEvent>
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OrderCompletedConsumer> _logger;

    public OrderCompletedConsumer(AppDbContext dbContext, ILogger<OrderCompletedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Order {OrderId} completed for User {UserId}, Total={Total}",
            msg.OrderId, msg.UserId, msg.Total);

        // Fake email notification (log to console)
        _logger.LogInformation("FAKE EMAIL: Order #{OrderId} completed! Total: ${Total}",
            msg.OrderId, msg.Total);

        // Idempotency: check if notification already exists
        var exists = await _dbContext.Notifications.AnyAsync(n =>
            n.OrderId == msg.OrderId &&
            n.Type == NotificationType.OrderCompleted);

        if (!exists)
        {
            var notification = new Notification
            {
                OrderId = msg.OrderId,
                Type = NotificationType.OrderCompleted,
                Message = $"Order #{msg.OrderId} completed successfully",
                Metadata = JsonSerializer.Serialize(new { msg.UserId, msg.Total }),
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Notifications.AddAsync(notification);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Notification saved for OrderId={OrderId}", msg.OrderId);
        }
        else
        {
            _logger.LogInformation("Notification already exists for OrderId={OrderId}", msg.OrderId);
        }
    }
}
