namespace CornerShop.Services
{
    /// <summary>
    /// Service for simulating controlled failures in saga orchestration
    /// </summary>
    public interface IControlledFailureService
    {
        /// <summary>
        /// Simulates a stock insufficiency failure
        /// </summary>
        Task<bool> SimulateInsufficientStockAsync(string productName, string storeId, int requestedQuantity, string? sagaId = null);

        /// <summary>
        /// Simulates a payment failure
        /// </summary>
        Task<bool> SimulatePaymentFailureAsync(decimal amount, string customerId, string? sagaId = null);

        /// <summary>
        /// Simulates a network timeout failure
        /// </summary>
        Task<bool> SimulateNetworkTimeoutAsync(string serviceName, string? sagaId = null);

        /// <summary>
        /// Simulates a database connection failure
        /// </summary>
        Task<bool> SimulateDatabaseFailureAsync(string operation, string? sagaId = null);

        /// <summary>
        /// Simulates a service unavailable failure
        /// </summary>
        Task<bool> SimulateServiceUnavailableAsync(string serviceName, string? sagaId = null);

        /// <summary>
        /// Gets the current failure configuration
        /// </summary>
        Dictionary<string, object> GetFailureConfiguration();

        /// <summary>
        /// Updates the failure configuration
        /// </summary>
        void UpdateFailureConfiguration(Dictionary<string, object> config);
    }
}
