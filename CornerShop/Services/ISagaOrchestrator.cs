using CornerShop.Models;

namespace CornerShop.Services
{
    public interface ISagaOrchestrator
    {
        Task<SagaResult> ExecuteSaleSagaAsync(CreateSaleRequest saleRequest);
        Task<SagaResult> ExecuteOrderSagaAsync(CreateOrderRequest orderRequest);
        Task<SagaResult> ExecuteStockUpdateSagaAsync(StockUpdateRequest stockRequest);
        Task<SagaResult> CompensateSagaAsync(string sagaId);
    }

    public class SagaResult
    {
        public bool IsSuccess { get; set; }
        public string SagaId { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public List<SagaStep> Steps { get; set; } = new();
        public List<CompensationResult> CompensationResults { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public bool HasCompensationFailures => CompensationResults.Any(r => !r.IsSuccessful);
        public int SuccessfulCompensations => CompensationResults.Count(r => r.IsSuccessful);
        public int FailedCompensations => CompensationResults.Count(r => !r.IsSuccessful);
    }

    public class SagaStep
    {
        public string StepId { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public bool IsCompensated { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ExecutedAt { get; set; }
        public object? Data { get; set; }
        public Func<Task>? CompensationAction { get; set; }
    }


}
