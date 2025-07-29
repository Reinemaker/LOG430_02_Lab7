using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using System.Text.Json;

namespace OrderService.Services
{
    /// <summary>
    /// Order Service Saga Participant - Handles saga participation for order operations
    /// </summary>
    public class OrderSagaParticipant : ISagaParticipant
    {
        private readonly IOrderService _orderService;
        private readonly IEventProducer _eventProducer;
        private readonly ILogger<OrderSagaParticipant> _logger;

        public string ServiceName => "OrderService";

        public List<string> SupportedSteps => new List<string>
        {
            "ConfirmOrder"
        };

        public OrderSagaParticipant(
            IOrderService orderService,
            IEventProducer eventProducer,
            ILogger<OrderSagaParticipant> logger)
        {
            _orderService = orderService;
            _eventProducer = eventProducer;
            _logger = logger;
        }

        public async Task<SagaParticipantResponse> ExecuteStepAsync(SagaParticipantRequest request)
        {
            _logger.LogInformation("Executing saga step: {SagaId} | {StepName} | {OrderId}", 
                request.SagaId, request.StepName, request.OrderId);

            try
            {
                switch (request.StepName)
                {
                    case "ConfirmOrder":
                        return await ExecuteConfirmOrderAsync(request);
                    default:
                        return new SagaParticipantResponse
                        {
                            SagaId = request.SagaId,
                            StepName = request.StepName,
                            Success = false,
                            ErrorMessage = $"Unsupported step: {request.StepName}",
                            CompensationRequired = false
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saga step execution failed: {SagaId} | {StepName}", request.SagaId, request.StepName);
                return new SagaParticipantResponse
                {
                    SagaId = request.SagaId,
                    StepName = request.StepName,
                    Success = false,
                    ErrorMessage = ex.Message,
                    CompensationRequired = false
                };
            }
        }

        public async Task<SagaCompensationResponse> CompensateStepAsync(SagaCompensationRequest request)
        {
            _logger.LogInformation("Compensating saga step: {SagaId} | {StepName} | Reason: {Reason}", 
                request.SagaId, request.StepName, request.Reason);

            try
            {
                switch (request.StepName)
                {
                    case "ConfirmOrder":
                        return await CompensateConfirmOrderAsync(request);
                    default:
                        return new SagaCompensationResponse
                        {
                            SagaId = request.SagaId,
                            StepName = request.StepName,
                            Success = false,
                            ErrorMessage = $"Unsupported compensation step: {request.StepName}"
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saga step compensation failed: {SagaId} | {StepName}", request.SagaId, request.StepName);
                return new SagaCompensationResponse
                {
                    SagaId = request.SagaId,
                    StepName = request.StepName,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<SagaParticipantResponse> ExecuteConfirmOrderAsync(SagaParticipantRequest request)
        {
            try
            {
                // Extract order data from request
                var orderData = ExtractOrderData(request.Data);
                if (orderData == null)
                {
                    return new SagaParticipantResponse
                    {
                        SagaId = request.SagaId,
                        StepName = request.StepName,
                        Success = false,
                        ErrorMessage = "Invalid order data",
                        CompensationRequired = false
                    };
                }

                // Create or update order
                var order = new Order
                {
                    Id = orderData.OrderId,
                    OrderNumber = GenerateOrderNumber(),
                    CustomerId = orderData.CustomerId,
                    StoreId = orderData.StoreId,
                    Status = OrderStatus.Confirmed,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Save order (in a real implementation, this would persist to database)
                var savedOrder = await _orderService.CreateOrderAsync(order);

                // Publish order confirmed event
                var orderConfirmedEvent = new OrderConfirmedEvent
                {
                    OrderId = order.Id,
                    ConfirmationTime = DateTime.UtcNow,
                    FinalAmount = 0, // Would be calculated from items
                    CorrelationId = request.CorrelationId
                };
                await _eventProducer.PublishOrderEventAsync(orderConfirmedEvent, request.CorrelationId);

                _logger.LogInformation("Order confirmed successfully: {SagaId} | {OrderId}", request.SagaId, order.Id);

                return new SagaParticipantResponse
                {
                    SagaId = request.SagaId,
                    StepName = request.StepName,
                    Success = true,
                    Data = new { OrderId = order.Id, OrderNumber = order.OrderNumber },
                    CompensationRequired = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order confirmation failed: {SagaId} | {OrderId}", request.SagaId, request.OrderId);
                return new SagaParticipantResponse
                {
                    SagaId = request.SagaId,
                    StepName = request.StepName,
                    Success = false,
                    ErrorMessage = ex.Message,
                    CompensationRequired = false
                };
            }
        }

        private async Task<SagaCompensationResponse> CompensateConfirmOrderAsync(SagaCompensationRequest request)
        {
            try
            {
                // Cancel the order
                var orderId = request.OrderId;
                if (string.IsNullOrEmpty(orderId))
                {
                    // Try to extract from data
                    var orderData = ExtractOrderData(request.Data);
                    orderId = orderData?.OrderId ?? "";
                }

                if (!string.IsNullOrEmpty(orderId))
                {
                    // In a real implementation, this would update the order status to cancelled
                    // await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Cancelled);

                    // Publish order cancelled event
                    var orderCancelledEvent = new OrderCancelledEvent
                    {
                        OrderId = orderId,
                        CancellationReason = request.Reason,
                        CancelledBy = "SagaCompensation",
                        CorrelationId = request.CorrelationId
                    };
                    await _eventProducer.PublishOrderEventAsync(orderCancelledEvent, request.CorrelationId);

                    _logger.LogInformation("Order compensation completed: {SagaId} | {OrderId} | Reason: {Reason}", 
                        request.SagaId, orderId, request.Reason);
                }

                return new SagaCompensationResponse
                {
                    SagaId = request.SagaId,
                    StepName = request.StepName,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order compensation failed: {SagaId} | {OrderId}", request.SagaId, request.OrderId);
                return new SagaCompensationResponse
                {
                    SagaId = request.SagaId,
                    StepName = request.StepName,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private OrderData? ExtractOrderData(object? data)
        {
            if (data == null) return null;

            try
            {
                var json = JsonSerializer.Serialize(data);
                return JsonSerializer.Deserialize<OrderData>(json);
            }
            catch
            {
                return null;
            }
        }

        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        private class OrderData
        {
            public string OrderId { get; set; } = string.Empty;
            public string CustomerId { get; set; } = string.Empty;
            public string StoreId { get; set; } = string.Empty;
        }
    }
} 