using CoderamaOpsAI.Common.Events;
using CoderamaOpsAI.Common.Interfaces;
using CoderamaOpsAI.Dal;
using CoderamaOpsAI.Dal.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoderamaOpsAI.Worker.BackgroundJobs;

public class OrderExpirationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderExpirationJob> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _expirationThreshold;

    public OrderExpirationJob(
        IServiceProvider serviceProvider,
        ILogger<OrderExpirationJob> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(
            configuration.GetValue<int>("OrderExpiration:IntervalSeconds", 60));
        _expirationThreshold = TimeSpan.FromMinutes(10);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderExpirationJob started (interval={Interval}s)",
            _interval.TotalSeconds);

        // Wait a bit before first execution to let services initialize
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredOrders(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OrderExpirationJob");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessExpiredOrders(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var cutoffTime = DateTime.UtcNow - _expirationThreshold;

        var expiredOrders = await dbContext.Orders
            .Where(o => o.Status == OrderStatus.Processing && o.UpdatedAt < cutoffTime)
            .ToListAsync(cancellationToken);

        if (expiredOrders.Count == 0)
        {
            _logger.LogDebug("No expired orders found (cutoff: {Cutoff})", cutoffTime);
            return;
        }

        _logger.LogInformation("Found {Count} expired orders", expiredOrders.Count);

        foreach (var order in expiredOrders)
        {
            order.Status = OrderStatus.Expired;
            order.UpdatedAt = DateTime.UtcNow;

            await eventBus.PublishAsync(new OrderExpiredEvent(order.Id, order.UserId),
                cancellationToken);

            _logger.LogInformation("Order {OrderId} marked as expired and event published", order.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
