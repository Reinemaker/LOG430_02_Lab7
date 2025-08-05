namespace CornerShop.Shared.Interfaces;

public interface IEventConsumer
{
    Task StartConsumingAsync();
    Task StopConsumingAsync();
}
