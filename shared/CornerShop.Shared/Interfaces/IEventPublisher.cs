namespace CornerShop.Shared.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, string topic) where T : Events.BaseEvent;
    Task PublishAsync<T>(T @event, string topic, string key) where T : Events.BaseEvent;
    Task PublishBatchAsync<T>(IEnumerable<T> events, string topic) where T : Events.BaseEvent;
} 