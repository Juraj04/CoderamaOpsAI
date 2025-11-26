using System.Text.Json;
using CoderamaOpsAI.Common.Events;
using CoderamaOpsAI.Dal;
using CoderamaOpsAI.Dal.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoderamaOpsAI.Worker.Consumers;

public class OrderExpiredConsumer : IConsumer<OrderExpiredEvent>
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OrderExpiredConsumer> _logger;

    public OrderExpiredConsumer(AppDbContext dbContext, ILogger<OrderExpiredConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderExpiredEvent> context)
    {
        var msg = context.Message;
        _logger.LogWarning("Order {OrderId} expired for User {UserId}", msg.OrderId, msg.UserId);

        // Idempotency: check if notification already exists
        var exists = await _dbContext.Notifications.AnyAsync(n =>
            n.OrderId == msg.OrderId &&
            n.Type == NotificationType.OrderExpired);

        if (!exists)
        {
            var notification = new Notification
            {
                OrderId = msg.OrderId,
                Type = NotificationType.OrderExpired,
                Message = $"Order #{msg.OrderId} expired after 10 minutes in Processing status",
                Metadata = JsonSerializer.Serialize(new { msg.UserId, ExpiredAt = DateTime.UtcNow }),
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Notifications.AddAsync(notification);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Expiration notification saved for OrderId={OrderId}", msg.OrderId);
        }
    }
}
