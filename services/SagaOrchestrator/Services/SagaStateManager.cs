using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace SagaOrchestrator.Services
{
    /// <summary>
    /// Saga State Manager - Handles saga state persistence and transitions
    /// </summary>
    public class SagaStateManager : ISagaStateManager
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<SagaStateManager> _logger;

        public SagaStateManager(IConnectionMultiplexer redis, ILogger<SagaStateManager> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task<string> CreateSagaAsync(string sagaType, string orderId, string? correlationId = null)
        {
            var sagaId = Guid.NewGuid().ToString();
            var db = _redis.GetDatabase();

            var sagaData = new
            {
                SagaId = sagaId,
                SagaType = sagaType,
                OrderId = orderId,
                CorrelationId = correlationId,
                CurrentState = SagaState.Started.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var sagaKey = $"saga:{sagaId}";
            var sagaJson = JsonSerializer.Serialize(sagaData);

            await db.StringSetAsync(sagaKey, sagaJson);
            await db.SetAddAsync("active_sagas", sagaId);

            _logger.LogInformation("Created saga: {SagaId} | {SagaType} | {OrderId}", sagaId, sagaType, orderId);

            return sagaId;
        }

        public async Task UpdateSagaStateAsync(string sagaId, SagaState newState, string serviceName, string action, SagaEventType eventType, string? message = null, object? data = null)
        {
            var db = _redis.GetDatabase();

            // Create state transition
            var transition = new SagaStateTransition
            {
                SagaId = sagaId,
                FromState = await GetSagaStateAsync(sagaId),
                ToState = newState,
                ServiceName = serviceName,
                Action = action,
                EventType = eventType,
                Message = message,
                Data = data
            };

            // Store transition
            var transitionKey = $"saga_transitions:{sagaId}";
            var transitionJson = JsonSerializer.Serialize(transition);
            await db.ListRightPushAsync(transitionKey, transitionJson);

            // Update saga state
            var sagaKey = $"saga:{sagaId}";
            var sagaJson = await db.StringGetAsync(sagaKey);

            if (!sagaJson.IsNull)
            {
                var sagaData = JsonSerializer.Deserialize<Dictionary<string, object>>(sagaJson.ToString());
                if (sagaData != null)
                {
                    sagaData["CurrentState"] = newState.ToString();
                    sagaData["UpdatedAt"] = DateTime.UtcNow;

                    var updatedSagaJson = JsonSerializer.Serialize(sagaData);
                    await db.StringSetAsync(sagaKey, updatedSagaJson);
                }
            }

            _logger.LogInformation("Updated saga state: {SagaId} | {FromState} -> {ToState} | {ServiceName} | {Action}",
                sagaId, transition.FromState, newState, serviceName, action);
        }

        public async Task<SagaState> GetSagaStateAsync(string sagaId)
        {
            var db = _redis.GetDatabase();
            var sagaKey = $"saga:{sagaId}";
            var sagaJson = await db.StringGetAsync(sagaKey);

            if (sagaJson.IsNull)
            {
                return SagaState.Started;
            }

            var sagaData = JsonSerializer.Deserialize<Dictionary<string, object>>(sagaJson.ToString());
            if (sagaData != null && sagaData.ContainsKey("CurrentState"))
            {
                var stateString = sagaData["CurrentState"].ToString();
                if (Enum.TryParse<SagaState>(stateString, out var state))
                {
                    return state;
                }
            }

            return SagaState.Started;
        }

        public async Task<List<SagaStateTransition>> GetSagaTransitionsAsync(string sagaId)
        {
            var db = _redis.GetDatabase();
            var transitionKey = $"saga_transitions:{sagaId}";
            var transitions = await db.ListRangeAsync(transitionKey);

            var result = new List<SagaStateTransition>();
            foreach (var transitionJson in transitions)
            {
                if (!transitionJson.IsNull)
                {
                    var transition = JsonSerializer.Deserialize<SagaStateTransition>(transitionJson.ToString());
                    if (transition != null)
                    {
                        result.Add(transition);
                    }
                }
            }

            return result.OrderBy(t => t.Timestamp).ToList();
        }

        public async Task CompleteSagaAsync(string sagaId, string result)
        {
            var db = _redis.GetDatabase();

            // Update saga with completion info
            var sagaKey = $"saga:{sagaId}";
            var sagaJson = await db.StringGetAsync(sagaKey);

            if (!sagaJson.IsNull)
            {
                var sagaData = JsonSerializer.Deserialize<Dictionary<string, object>>(sagaJson.ToString());
                if (sagaData != null)
                {
                    sagaData["Result"] = result;
                    sagaData["CompletedAt"] = DateTime.UtcNow;
                    sagaData["UpdatedAt"] = DateTime.UtcNow;

                    var updatedSagaJson = JsonSerializer.Serialize(sagaData);
                    await db.StringSetAsync(sagaKey, updatedSagaJson);
                }
            }

            // Remove from active sagas
            await db.SetRemoveAsync("active_sagas", sagaId);

            // Add to completed sagas
            await db.SetAddAsync("completed_sagas", sagaId);

            _logger.LogInformation("Completed saga: {SagaId} | Result: {Result}", sagaId, result);
        }

        public async Task<List<string>> GetActiveSagasAsync()
        {
            var db = _redis.GetDatabase();
            var activeSagas = await db.SetMembersAsync("active_sagas");

            return activeSagas
                .Where(s => !s.IsNull)
                .Select(s => s.ToString())
                .ToList();
        }
    }
}
