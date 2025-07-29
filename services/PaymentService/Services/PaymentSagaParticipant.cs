using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using System.Text.Json;

namespace PaymentService.Services;

public class PaymentSagaParticipant : ISagaParticipant
{
    private readonly IEventProducer _eventProducer;
    private readonly ILogger<PaymentSagaParticipant> _logger;
    private readonly IDistributedCache _cache;

    public PaymentSagaParticipant(
        IEventProducer eventProducer,
        ILogger<PaymentSagaParticipant> logger,
        IDistributedCache cache)
    {
        _eventProducer = eventProducer;
        _logger = logger;
        _cache = cache;
    }

    public string ServiceName => "PaymentService";

    public List<string> SupportedSteps => new List<string> { "ProcessPayment" };

    public async Task<SagaParticipantResponse> ExecuteStepAsync(SagaParticipantRequest request)
    {
        _logger.LogInformation("Executing saga step {StepName} for correlation {CorrelationId}", 
            request.StepName, request.CorrelationId);

        try
        {
            return request.StepName switch
            {
                "ProcessPayment" => await ExecuteProcessPaymentAsync(request),
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
                "ProcessPayment" => await CompensateProcessPaymentAsync(request),
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

    private async Task<SagaParticipantResponse> ExecuteProcessPaymentAsync(SagaParticipantRequest request)
    {
        var orderData = JsonSerializer.Deserialize<Dictionary<string, object>>(request.Data.ToString() ?? "{}");
        var amount = Convert.ToDecimal(orderData.GetValueOrDefault("amount") ?? 0);
        var paymentMethod = orderData.GetValueOrDefault("paymentMethod")?.ToString() ?? "CreditCard";
        var customerId = orderData.GetValueOrDefault("customerId")?.ToString();

        if (amount <= 0 || string.IsNullOrEmpty(customerId))
        {
            return new SagaParticipantResponse 
            { 
                Success = false, 
                ErrorMessage = "Invalid payment amount or customer ID" 
            };
        }

        // Simulate payment processing with controlled failures
        var paymentSuccess = await SimulatePaymentProcessingAsync(amount, paymentMethod, customerId);
        
        if (paymentSuccess)
        {
            var paymentProcessedEvent = new PaymentProcessedEvent
            {
                CustomerId = customerId,
                Amount = amount,
                PaymentMethod = paymentMethod,
                TransactionId = Guid.NewGuid().ToString(),
                ProcessedAt = DateTime.UtcNow
            };

            await _eventProducer.PublishPaymentEventAsync(paymentProcessedEvent, request.CorrelationId);

            // Store payment record for potential compensation
            var paymentKey = $"payment:{request.CorrelationId}";
            var paymentRecord = new { CustomerId = customerId, Amount = amount, PaymentMethod = paymentMethod, TransactionId = paymentProcessedEvent.TransactionId };
            await _cache.SetStringAsync(paymentKey, JsonSerializer.Serialize(paymentRecord), 
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) });

            return new SagaParticipantResponse 
            { 
                Success = true, 
                Data = new { TransactionId = paymentProcessedEvent.TransactionId, Amount = amount }
            };
        }
        else
        {
            var paymentFailedEvent = new PaymentFailedEvent
            {
                CustomerId = customerId,
                Amount = amount,
                PaymentMethod = paymentMethod,
                FailureReason = "Payment processing failed",
                FailedAt = DateTime.UtcNow
            };

            await _eventProducer.PublishPaymentEventAsync(paymentFailedEvent, request.CorrelationId);

            return new SagaParticipantResponse 
            { 
                Success = false, 
                ErrorMessage = "Payment processing failed" 
            };
        }
    }

    private async Task<SagaCompensationResponse> CompensateProcessPaymentAsync(SagaCompensationRequest request)
    {
        var orderData = JsonSerializer.Deserialize<Dictionary<string, object>>(request.Data.ToString() ?? "{}");
        var customerId = orderData.GetValueOrDefault("customerId")?.ToString();
        var amount = Convert.ToDecimal(orderData.GetValueOrDefault("amount") ?? 0);

        if (string.IsNullOrEmpty(customerId))
        {
            return new SagaCompensationResponse 
            { 
                Success = false, 
                ErrorMessage = "Invalid customer ID for payment compensation" 
            };
        }

        // Simulate payment refund
        var paymentKey = $"payment:{request.CorrelationId}";
        var paymentRecordJson = await _cache.GetStringAsync(paymentKey);
        
        if (!string.IsNullOrEmpty(paymentRecordJson))
        {
            var paymentRecord = JsonSerializer.Deserialize<Dictionary<string, object>>(paymentRecordJson);
            var transactionId = paymentRecord?.GetValueOrDefault("transactionId")?.ToString();

            // Simulate refund processing
            var refundSuccess = await SimulateRefundProcessingAsync(transactionId ?? "", amount);

            if (refundSuccess)
            {
                // Remove payment record after successful refund
                await _cache.RemoveAsync(paymentKey);

                return new SagaCompensationResponse 
                { 
                    Success = true, 
                    Data = new { Refunded = true, TransactionId = transactionId, Amount = amount }
                };
            }
            else
            {
                return new SagaCompensationResponse 
                { 
                    Success = false, 
                    ErrorMessage = "Payment refund failed" 
                };
            }
        }

        return new SagaCompensationResponse 
        { 
            Success = false, 
            ErrorMessage = "Payment record not found for compensation" 
        };
    }

    private async Task<bool> SimulatePaymentProcessingAsync(decimal amount, string paymentMethod, string customerId)
    {
        // Simulate payment processing with controlled failures for testing
        await Task.Delay(100); // Simulate processing time

        // Simulate different failure scenarios
        if (amount > 1000 && paymentMethod == "CreditCard")
        {
            // Simulate high amount failure
            return false;
        }

        if (customerId?.EndsWith("_failed") == true)
        {
            // Simulate customer-specific failure
            return false;
        }

        // Random failure simulation (10% failure rate)
        var random = new Random();
        return random.Next(1, 11) > 1;
    }

    private async Task<bool> SimulateRefundProcessingAsync(string transactionId, decimal amount)
    {
        // Simulate refund processing
        await Task.Delay(50); // Simulate processing time
        
        // Simulate successful refund (90% success rate)
        var random = new Random();
        return random.Next(1, 11) > 1;
    }
} 