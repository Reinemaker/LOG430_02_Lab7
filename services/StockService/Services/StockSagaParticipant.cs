using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using System.Text.Json;

namespace StockService.Services;

public class StockSagaParticipant : ISagaParticipant
{
    private readonly IEventProducer _eventProducer;
    private readonly ILogger<StockSagaParticipant> _logger;
    private readonly IDistributedCache _cache;

    public StockSagaParticipant(
        IEventProducer eventProducer,
        ILogger<StockSagaParticipant> logger,
        IDistributedCache cache)
    {
        _eventProducer = eventProducer;
        _logger = logger;
        _cache = cache;
    }

    public string ServiceName => "StockService";

    public List<string> SupportedSteps => new List<string> { "VerifyStock", "ReserveStock" };

    public async Task<SagaParticipantResponse> ExecuteStepAsync(SagaParticipantRequest request)
    {
        _logger.LogInformation("Executing saga step {StepName} for correlation {CorrelationId}",
            request.StepName, request.CorrelationId);

        try
        {
            return request.StepName switch
            {
                "VerifyStock" => await ExecuteVerifyStockAsync(request),
                "ReserveStock" => await ExecuteReserveStockAsync(request),
                _ => new SagaParticipantResponse
                {
                    Success = false,
                    ErrorMessage = $"Unsupported step: {request.StepName}"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing saga step {StepName}", request.StepName);
            return new SagaParticipantResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<SagaCompensationResponse> CompensateStepAsync(SagaCompensationRequest request)
    {
        _logger.LogInformation("Compensating saga step {StepName} for correlation {CorrelationId}",
            request.StepName, request.CorrelationId);

        try
        {
            return request.StepName switch
            {
                "ReserveStock" => await CompensateReserveStockAsync(request),
                _ => new SagaCompensationResponse
                {
                    Success = false,
                    ErrorMessage = $"Unsupported compensation step: {request.StepName}"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compensating saga step {StepName}", request.StepName);
            return new SagaCompensationResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<SagaParticipantResponse> ExecuteVerifyStockAsync(SagaParticipantRequest request)
    {
        var orderData = JsonSerializer.Deserialize<Dictionary<string, object>>(request.Data.ToString() ?? "{}");
        var productId = orderData.GetValueOrDefault("productId")?.ToString();
        var quantity = Convert.ToInt32(orderData.GetValueOrDefault("quantity") ?? 0);

        if (string.IsNullOrEmpty(productId) || quantity <= 0)
        {
            return new SagaParticipantResponse
            {
                Success = false,
                ErrorMessage = "Invalid product ID or quantity"
            };
        }

        // Simulate stock verification
        var availableStock = await GetAvailableStockAsync(productId);

        if (availableStock >= quantity)
        {
            var stockVerifiedEvent = new StockVerifiedEvent
            {
                ProductId = productId,
                RequestedQuantity = quantity,
                AvailableStock = availableStock,
                VerificationResult = true
            };

            await _eventProducer.PublishInventoryEventAsync(stockVerifiedEvent, request.CorrelationId);

            return new SagaParticipantResponse
            {
                Success = true,
                Data = new { AvailableStock = availableStock, VerificationResult = true }
            };
        }
        else
        {
            var stockVerifiedEvent = new StockVerifiedEvent
            {
                ProductId = productId,
                RequestedQuantity = quantity,
                AvailableStock = availableStock,
                VerificationResult = false
            };

            await _eventProducer.PublishInventoryEventAsync(stockVerifiedEvent, request.CorrelationId);

            return new SagaParticipantResponse
            {
                Success = false,
                ErrorMessage = $"Insufficient stock. Available: {availableStock}, Requested: {quantity}"
            };
        }
    }

    private async Task<SagaParticipantResponse> ExecuteReserveStockAsync(SagaParticipantRequest request)
    {
        var orderData = JsonSerializer.Deserialize<Dictionary<string, object>>(request.Data.ToString() ?? "{}");
        var productId = orderData.GetValueOrDefault("productId")?.ToString();
        var quantity = Convert.ToInt32(orderData.GetValueOrDefault("quantity") ?? 0);

        if (string.IsNullOrEmpty(productId) || quantity <= 0)
        {
            return new SagaParticipantResponse
            {
                Success = false,
                ErrorMessage = "Invalid product ID or quantity"
            };
        }

        // Simulate stock reservation
        var reservationKey = $"reservation:{productId}:{request.CorrelationId}";
        var reservationData = new { ProductId = productId, Quantity = quantity, ReservedAt = DateTime.UtcNow };

        await _cache.SetStringAsync(reservationKey, JsonSerializer.Serialize(reservationData),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) });

        var stockReservedEvent = new StockReservedEvent
        {
            ProductId = productId,
            ReservedQuantity = quantity,
            ReservationId = reservationKey
        };

        await _eventProducer.PublishInventoryEventAsync(stockReservedEvent, request.CorrelationId);

        return new SagaParticipantResponse
        {
            Success = true,
            Data = new { ReservationId = reservationKey, ReservedQuantity = quantity }
        };
    }

    private async Task<SagaCompensationResponse> CompensateReserveStockAsync(SagaCompensationRequest request)
    {
        var orderData = JsonSerializer.Deserialize<Dictionary<string, object>>(request.Data.ToString() ?? "{}");
        var productId = orderData.GetValueOrDefault("productId")?.ToString();
        var reservationId = orderData.GetValueOrDefault("reservationId")?.ToString();

        if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(reservationId))
        {
            return new SagaCompensationResponse
            {
                Success = false,
                ErrorMessage = "Invalid product ID or reservation ID"
            };
        }

        // Simulate stock release
        await _cache.RemoveAsync(reservationId);

        var stockReleasedEvent = new StockReleasedEvent
        {
            ProductId = productId,
            ReservationId = reservationId,
            ReleasedQuantity = Convert.ToInt32(orderData.GetValueOrDefault("quantity") ?? 0)
        };

        await _eventProducer.PublishInventoryEventAsync(stockReleasedEvent, request.CorrelationId);

        return new SagaCompensationResponse
        {
            Success = true,
            Data = new { Released = true, ReservationId = reservationId }
        };
    }

    private async Task<int> GetAvailableStockAsync(string productId)
    {
        // Simulate stock lookup - in real implementation, this would query a database
        var stockKey = $"stock:{productId}";
        var cachedStock = await _cache.GetStringAsync(stockKey);

        if (int.TryParse(cachedStock, out var stock))
        {
            return stock;
        }

        // Simulate random stock availability for testing
        var randomStock = new Random().Next(0, 100);
        await _cache.SetStringAsync(stockKey, randomStock.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });

        return randomStock;
    }
}
