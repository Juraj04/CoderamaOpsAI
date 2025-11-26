using CoderamaOpsAI.Common.Configuration;
using CoderamaOpsAI.Dal;
using CoderamaOpsAI.Worker.BackgroundJobs;
using CoderamaOpsAI.Worker.Consumers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Add DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));

        // Add MassTransit with consumers
        services.AddEventBus(context.Configuration, x =>
        {
            x.AddConsumer<OrderCreatedConsumer>();
            x.AddConsumer<OrderCompletedConsumer>();
            x.AddConsumer<OrderExpiredConsumer>();
        });

        // Add background job
        services.AddHostedService<OrderExpirationJob>();
    });

await builder.Build().RunAsync();
