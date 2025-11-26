namespace CoderamaOpsAI.Common.Interfaces;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken) where T : class;
}
