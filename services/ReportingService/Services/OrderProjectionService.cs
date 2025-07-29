using Confluent.Kafka;
using MongoDB.Driver;
using ReportingService.Models;
using System.Text.Json;

namespace ReportingService.Services;

public class OrderProjectionService : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IMongoCollection<OrderReadModel> _orderReadModels;
    private readonly ILogger<OrderProjectionService> _logger;
    private readonly Dictionary<string, Func<string, Task>> _eventHandlers;

    public OrderProjectionService(IConfiguration configuration, IMongoDatabase database, ILogger<OrderProjectionService> logger)
    {
        _logger = logger;
        _orderReadModels = database.GetCollection<OrderReadModel>("orderReadModels");

        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "kafka:29092",
            GroupId = "reporting-service-projections",
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

        // Create indexes for better query performance
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        var orderIdIndex = Builders<OrderReadModel>.IndexKeys.Ascending(o => o.OrderId);
        var orderIdIndexModel = new CreateIndexModel<OrderReadModel>(orderIdIndex, new CreateIndexOptions { Name = "OrderId", Unique = true });
        _orderReadModels.Indexes.CreateOne(orderIdIndexModel);

        var customerIdIndex = Builders<OrderReadModel>.IndexKeys.Ascending(o => o.CustomerId);
        var customerIdIndexModel = new CreateIndexModel<OrderReadModel>(customerIdIndex, new CreateIndexOptions { Name = "CustomerId" });
        _orderReadModels.Indexes.CreateOne(customerIdIndexModel);

        var statusIndex = Builders<OrderReadModel>.IndexKeys.Ascending(o => o.Status);
        var statusIndexModel = new CreateIndexModel<OrderReadModel>(statusIndex, new CreateIndexOptions { Name = "Status" });
        _orderReadModels.Indexes.CreateOne(statusIndexModel);

        var createdAtIndex = Builders<OrderReadModel>.IndexKeys.Descending(o => o.CreatedAt);
        var createdAtIndexModel = new CreateIndexModel<OrderReadModel>(createdAtIndex, new CreateIndexOptions { Name = "CreatedAt" });
        _orderReadModels.Indexes.CreateOne(createdAtIndexModel);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(new[] { "orders.events", "payments.events" });

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    
                    _logger.LogInformation("Processing event for projection: {Topic} {Partition} {Offset}",
                        consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);

                    await ProcessMessageAsync(consumeResult);
                    
                    _consumer.Commit(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message for projection");
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
                _logger.LogWarning("No projection handler found for event type: {EventType}", eventType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for projection: {Message}", consumeResult.Message.Value);
        }
    }

    private async Task HandleOrderCreated(string eventJson)
    {
        var eventData = JsonSerializer.Deserialize<JsonElement>(eventJson);
        var data = eventData.GetProperty("data");
        
        var orderId = data.GetProperty("orderId").GetString();
        var customerId = data.GetProperty("customerId").GetString();
        var totalAmount = data.GetProperty("totalAmount").GetDecimal();
        var items = data.GetProperty("items").EnumerateArray()
            .Select(item => new OrderItemReadModel
            {
                ProductId = item.GetProperty("productId").GetString() ?? string.Empty,
                ProductName = item.GetProperty("productName").GetString() ?? string.Empty,
                Quantity = item.GetProperty("quantity").GetInt32(),
                UnitPrice = item.GetProperty("unitPrice").GetDecimal(),
                TotalPrice = item.GetProperty("totalPrice").GetDecimal()
            })
            .ToList();

        var orderReadModel = new OrderReadModel
        {
            OrderId = orderId ?? string.Empty,
            CustomerId = customerId ?? string.Empty,
            Status = "Created",
            TotalAmount = totalAmount,
            Items = items,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var filter = Builders<OrderReadModel>.Filter.Eq(o => o.OrderId, orderId);
        var update = Builders<OrderReadModel>.Update
            .SetOnInsert(o => o.OrderId, orderId)
            .Set(o => o.CustomerId, customerId)
            .Set(o => o.Status, "Created")
            .Set(o => o.TotalAmount, totalAmount)
            .Set(o => o.Items, items)
            .Set(o => o.CreatedAt, DateTime.UtcNow)
            .Set(o => o.UpdatedAt, DateTime.UtcNow);

        await _orderReadModels.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        
        _logger.LogInformation("Order read model created/updated for order {OrderId}", orderId);
    }

    private async Task HandleOrderConfirmed(string eventJson)
    {
        var eventData = JsonSerializer.Deserialize<JsonElement>(eventJson);
        var data = eventData.GetProperty("data");
        
        var orderId = data.GetProperty("orderId").GetString();
        var paymentId = data.GetProperty("paymentId").GetString();

        var filter = Builders<OrderReadModel>.Filter.Eq(o => o.OrderId, orderId);
        var update = Builders<OrderReadModel>.Update
            .Set(o => o.Status, "Confirmed")
            .Set(o => o.PaymentId, paymentId)
            .Set(o => o.ConfirmedAt, DateTime.UtcNow)
            .Set(o => o.UpdatedAt, DateTime.UtcNow);

        await _orderReadModels.UpdateOneAsync(filter, update);
        
        _logger.LogInformation("Order read model updated - confirmed for order {OrderId}", orderId);
    }

    private async Task HandleOrderShipped(string eventJson)
    {
        var eventData = JsonSerializer.Deserialize<JsonElement>(eventJson);
        var data = eventData.GetProperty("data");
        
        var orderId = data.GetProperty("orderId").GetString();
        var trackingNumber = data.GetProperty("trackingNumber").GetString();
        var carrier = data.GetProperty("carrier").GetString();

        var filter = Builders<OrderReadModel>.Filter.Eq(o => o.OrderId, orderId);
        var update = Builders<OrderReadModel>.Update
            .Set(o => o.Status, "Shipped")
            .Set(o => o.TrackingNumber, trackingNumber)
            .Set(o => o.Carrier, carrier)
            .Set(o => o.ShippedAt, DateTime.UtcNow)
            .Set(o => o.UpdatedAt, DateTime.UtcNow);

        await _orderReadModels.UpdateOneAsync(filter, update);
        
        _logger.LogInformation("Order read model updated - shipped for order {OrderId}", orderId);
    }

    private async Task HandleOrderDelivered(string eventJson)
    {
        var eventData = JsonSerializer.Deserialize<JsonElement>(eventJson);
        var data = eventData.GetProperty("data");
        
        var orderId = data.GetProperty("orderId").GetString();

        var filter = Builders<OrderReadModel>.Filter.Eq(o => o.OrderId, orderId);
        var update = Builders<OrderReadModel>.Update
            .Set(o => o.Status, "Delivered")
            .Set(o => o.DeliveredAt, DateTime.UtcNow)
            .Set(o => o.UpdatedAt, DateTime.UtcNow);

        await _orderReadModels.UpdateOneAsync(filter, update);
        
        _logger.LogInformation("Order read model updated - delivered for order {OrderId}", orderId);
    }

    private async Task HandlePaymentCompleted(string eventJson)
    {
        var eventData = JsonSerializer.Deserialize<JsonElement>(eventJson);
        var data = eventData.GetProperty("data");
        
        var paymentId = data.GetProperty("paymentId").GetString();
        var amount = data.GetProperty("amount").GetDecimal();

        // Find orders with this payment ID and update payment status
        var filter = Builders<OrderReadModel>.Filter.Eq(o => o.PaymentId, paymentId);
        var update = Builders<OrderReadModel>.Update
            .Set(o => o.PaymentStatus, "Completed")
            .Set(o => o.UpdatedAt, DateTime.UtcNow);

        var result = await _orderReadModels.UpdateManyAsync(filter, update);
        
        _logger.LogInformation("Payment status updated for {Count} orders with payment {PaymentId}", result.ModifiedCount, paymentId);
    }

    private async Task HandlePaymentFailed(string eventJson)
    {
        var eventData = JsonSerializer.Deserialize<JsonElement>(eventJson);
        var data = eventData.GetProperty("data");
        
        var paymentId = data.GetProperty("paymentId").GetString();

        // Find orders with this payment ID and update payment status
        var filter = Builders<OrderReadModel>.Filter.Eq(o => o.PaymentId, paymentId);
        var update = Builders<OrderReadModel>.Update
            .Set(o => o.PaymentStatus, "Failed")
            .Set(o => o.UpdatedAt, DateTime.UtcNow);

        var result = await _orderReadModels.UpdateManyAsync(filter, update);
        
        _logger.LogInformation("Payment status updated for {Count} orders with payment {PaymentId}", result.ModifiedCount, paymentId);
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
} 