using CornerShop.Shared.Events;
using CornerShop.Shared.Models;

namespace CornerShop.Shared.Interfaces;

public interface IChoreographedSagaCoordinator
{
    Task HandleOrderCreatedEventAsync(OrderCreatedEvent orderCreatedEvent);
    Task HandleStockReservedEventAsync(StockReservedEvent stockReservedEvent);
    Task HandlePaymentProcessedEventAsync(PaymentProcessedEvent paymentProcessedEvent);
    Task HandleOrderConfirmedEventAsync(OrderConfirmedEvent orderConfirmedEvent);
    Task HandleNotificationSentEventAsync(NotificationSentEvent notificationSentEvent);
    Task HandleOrderCancelledEventAsync(OrderCancelledEvent orderCancelledEvent);
    Task HandleStockReleasedEventAsync(StockReleasedEvent stockReleasedEvent);
    Task HandlePaymentRefundedEventAsync(PaymentRefundedEvent paymentRefundedEvent);
    Task<ChoreographedSagaState?> GetSagaStateAsync(string sagaId);
    Task<List<ChoreographedSagaState>> GetAllSagaStatesAsync();
}
