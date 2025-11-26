using CoderamaOpsAI.Common.Interfaces;
using CoderamaOpsAI.Common.Services;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoderamaOpsAI.Common.Configuration;

public static class MassTransitConfiguration
{
    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        var rabbitMqSettings = configuration.GetSection("RabbitMq").Get<RabbitMqSettings>()
            ?? throw new InvalidOperationException("RabbitMq configuration missing");

        services.AddMassTransit(x =>
        {
            // Register consumers if provided (Worker only)
            configureConsumers?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqSettings.Host, (ushort)rabbitMqSettings.Port, rabbitMqSettings.VirtualHost, h =>
                {
                    h.Username(rabbitMqSettings.Username);
                    h.Password(rabbitMqSettings.Password);
                });

                // Retry policy: 3 attempts, exponential backoff
                cfg.UseMessageRetry(r => r.Exponential(
                    retryLimit: 3,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(2)
                ));

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IEventBus, EventBus>();
        services.AddSingleton<IPaymentSimulator, PaymentSimulator>();

        return services;
    }
}
