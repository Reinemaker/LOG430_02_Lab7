using Confluent.Kafka;
using System.Text.Json;

namespace NotificationService.Services;

public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendSMSAsync(string to, string message);
    Task SendPushNotificationAsync(string userId, string title, string message);
}

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // Simulate email sending
        await Task.Delay(100);
        _logger.LogInformation("EMAIL SENT: To: {To}, Subject: {Subject}", to, subject);
        
        // In a real implementation, you would integrate with an email service like SendGrid, AWS SES, etc.
    }

    public async Task SendSMSAsync(string to, string message)
    {
        // Simulate SMS sending
        await Task.Delay(50);
        _logger.LogInformation("SMS SENT: To: {To}, Message: {Message}", to, message);
        
        // In a real implementation, you would integrate with an SMS service like Twilio, AWS SNS, etc.
    }

    public async Task SendPushNotificationAsync(string userId, string title, string message)
    {
        // Simulate push notification sending
        await Task.Delay(75);
        _logger.LogInformation("PUSH NOTIFICATION SENT: User: {UserId}, Title: {Title}", userId, title);
        
        // In a real implementation, you would integrate with push notification services like Firebase, etc.
    }
}

public class EventConsumerService : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly INotificationService _notificationService;
    private readonly ILogger<EventConsumerService> _logger;
    private readonly Dictionary<string, Func<string, Task>> _eventHandlers;

    public EventConsumerService(IConfiguration configuration, INotificationService notificationService, ILogger<EventConsumerService> logger)
    {
        _notificationService = notificationService;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "kafka:29092",
            GroupId = "notification-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        
        _eventHandlers = new Dictionary<string, Func<string, Task>>
        {
            { "OrderCreated", HandleOrderCreated },
            { "OrderConfirmed", HandleOrderConfirmed },
            { "OrderShipped", HandleOrderShipped },
            { "OrderDelivered", HandleOrderDelivered },
            { "PaymentCompleted", HandlePaymentCompleted },
            { "PaymentFailed", HandlePaymentFailed }
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(new[] { "orders.events", "payments.events", "carts.events" });

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    
                    _logger.LogInformation("Received message: {Topic} {Partition} {Offset} {Key}",
                        consumeResult.Topic, consumeResult.Partition, consumeResult.Offset, consumeResult.Message.Key);

                    await ProcessMessageAsync(consumeResult);
                    
                    _consumer.Commit(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message");
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult)
    {
        try
        {
            var eventData = JsonSerializer.Deserialize<JsonElement>(consumeResult.Message.Value);
            var eventType = eventData.GetProperty("eventType").GetString();

            if (eventType != null && _eventHandlers.ContainsKey(eventType))
            {
                await _eventHandlers[eventType](consumeResult.Message.Value);
            }
            else
            {
                _logger.LogWarning("No handler found for event type: {EventType}", eventType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {Message}", consumeResult.Message.Value);
        }
    }

    private async Task HandleOrderCreated(string eventJson)
    {
        var eventData = JsonSerializer.Deserialize<JsonElement>(eventJson);
        var data = eventData.GetProperty("data");
        
        var orderId = data.GetProperty("orderId").GetString();
        var customerId = data.GetProperty("customerId").GetString();
        var totalAmount = data.GetProperty("totalAmount").GetDecimal();

        var emailSubject = "Order Confirmation";
        var emailBody = $@"
            <h2>Thank you for your order!</h2>
            <p>Order ID: {orderId}</p>
            <p>Total Amount: ${totalAmount}</p>
            <p>We'll notify you when your order is ready to ship.</p>";

        await _notificationService.SendEmailAsync($"customer-{customerId}@example.com", emailSubject, emailBody);
        
        _logger.LogInformation("Order created notification sent for order {OrderId}", orderId);
    }

    private async Task HandleOrderConfirmed(string eventJson)
    {
        var eventData = JsonSerializer.Deserialize<JsonElement>(eventJson);
        var data = eventData.GetProperty("data");
        
        var orderId = data.GetProperty("orderId").GetString();
        var paymentId = data.GetProperty("paymentId").GetString();

        var emailSubject = "Order Confirmed";
        var emailBody = $@"
            <h2>Your order has been confirmed!</h2>
            <p>Order ID: {orderId}</p>
            <p>Payment ID: {paymentId}</p>
            <p>We're preparing your order for shipment.</p>";

        // In a real implementation, you would get the customer email from a database
        await _notificationService.SendEmailAsync("customer@example.com", emailSubject, emailBody);
        
        _logger.LogInformation("Order confirmed notification sent for order {OrderId}", orderId);
    }

    private async Task HandleOrderShipped(string eventJson)
    {
        var eventData = JsonSerializer.Deserialize<JsonElement>(eventJson);
        var data = eventData.GetProperty("data");
        
        var orderId = data.GetProperty("orderId").GetString();
        var trackingNumber = data.GetProperty("trackingNumber").GetString();
        var carrier = data.GetProperty("carrier").GetString();

        var emailSubject = "Your order has shipped!";
        var emailBody = $@"
            <h2>Your order is on its way!</h2>
            <p>Order ID: {orderId}</p>
            <p>Tracking Number: {trackingNumber}</p>
            <p>Carrier: {carrier}</p>
            <p>Track your package: https://tracking.example.com/{trackingNumber}</p>";

        await _notificationService.SendEmailAsync("customer@example.com", emailSubject, emailBody);
        
        // Send SMS notification
        await _notificationService.SendSMSAsync("+1234567890", $"Your order {orderId} has shipped! Track: {trackingNumber}");
        
        _logger.LogInformation("Order shipped notification sent for order {OrderId}", orderId);
    }

    private async Task HandleOrderDelivered(string eventJson)
    {
        var eventData = JsonSerializer.Deserialize<JsonElement>(eventJson);
        var data = eventData.GetProperty("data");
        
        var orderId = data.GetProperty("orderId").GetString();

        var emailSubject = "Order Delivered";
        var emailBody = $@"
            <h2>Your order has been delivered!</h2>
            <p>Order ID: {orderId}</p>
            <p>Thank you for shopping with us!</p>";

        await _notificationService.SendEmailAsync("customer@example.com", emailSubject, emailBody);
        
        // Send push notification
        await _notificationService.SendPushNotificationAsync("user-123", "Order Delivered", $"Your order {orderId} has been delivered!");
        
        _logger.LogInformation("Order delivered notification sent for order {OrderId}", orderId);
    }

    private async Task HandlePaymentCompleted(string eventJson)
    {
        var eventData = JsonSerializer.Deserialize<JsonElement>(eventJson);
        var data = eventData.GetProperty("data");
        
        var paymentId = data.GetProperty("paymentId").GetString();
        var amount = data.GetProperty("amount").GetDecimal();

        var emailSubject = "Payment Confirmed";
        var emailBody = $@"
            <h2>Payment Confirmed</h2>
            <p>Payment ID: {paymentId}</p>
            <p>Amount: ${amount}</p>
            <p>Your payment has been processed successfully.</p>";

        await _notificationService.SendEmailAsync("customer@example.com", emailSubject, emailBody);
        
        _logger.LogInformation("Payment completed notification sent for payment {PaymentId}", paymentId);
    }

    private async Task HandlePaymentFailed(string eventJson)
    {
        var eventData = JsonSerializer.Deserialize<JsonElement>(eventJson);
        var data = eventData.GetProperty("data");
        
        var paymentId = data.GetProperty("paymentId").GetString();
        var reason = data.GetProperty("reason").GetString();

        var emailSubject = "Payment Failed";
        var emailBody = $@"
            <h2>Payment Failed</h2>
            <p>Payment ID: {paymentId}</p>
            <p>Reason: {reason}</p>
            <p>Please try again or contact support.</p>";

        await _notificationService.SendEmailAsync("customer@example.com", emailSubject, emailBody);
        
        // Send SMS for urgent payment failure
        await _notificationService.SendSMSAsync("+1234567890", $"Payment failed for {paymentId}. Please check your email.");
        
        _logger.LogInformation("Payment failed notification sent for payment {PaymentId}", paymentId);
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
} 