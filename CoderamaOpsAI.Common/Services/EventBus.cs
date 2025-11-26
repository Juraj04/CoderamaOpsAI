using CoderamaOpsAI.Common.Interfaces;
using MassTransit;

namespace CoderamaOpsAI.Common.Services;

public class EventBus : IEventBus
{
    private readonly IPublishEndpoint _publishEndpoint;

    public EventBus(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken) where T : class
    {
        await _publishEndpoint.Publish(@event, cancellationToken);
    }
}
