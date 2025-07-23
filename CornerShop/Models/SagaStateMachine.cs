using System.ComponentModel.DataAnnotations;

namespace CornerShop.Models
{
    /// <summary>
    /// Explicit state machine for saga execution
    /// </summary>
    public enum SagaState
    {
        [Display(Name = "Started")]
        Started = 0,

        [Display(Name = "Store Validated")]
        StoreValidated = 1,

        [Display(Name = "Stock Reserved")]
        StockReserved = 2,

        [Display(Name = "Total Calculated")]
        TotalCalculated = 3,

        [Display(Name = "Sale Created")]
        SaleCreated = 4,

        [Display(Name = "Stock Confirmed")]
        StockConfirmed = 5,

        [Display(Name = "Completed")]
        Completed = 6,

        [Display(Name = "Failed")]
        Failed = 7,

        [Display(Name = "Compensating")]
        Compensating = 8,

        [Display(Name = "Compensated")]
        Compensated = 9
    }

    /// <summary>
    /// Event types that microservices can publish
    /// </summary>
    public enum SagaEventType
    {
        [Display(Name = "Success")]
        Success = 0,

        [Display(Name = "Failure")]
        Failure = 1
    }

    /// <summary>
    /// Saga state transition record
    /// </summary>
    public class SagaStateTransition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SagaId { get; set; } = string.Empty;
        public SagaState FromState { get; set; }
        public SagaState ToState { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public SagaEventType EventType { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public object? Data { get; set; }
    }

    /// <summary>
    /// Saga state machine with persistence
    /// </summary>
    public class SagaStateMachine
    {
        public string SagaId { get; set; } = string.Empty;
        public SagaState CurrentState { get; set; } = SagaState.Started;
        public string SagaType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public List<SagaStateTransition> Transitions { get; set; } = new();
        public bool IsCompleted => CurrentState == SagaState.Completed || CurrentState == SagaState.Compensated;
        public bool IsFailed => CurrentState == SagaState.Failed;
        public bool IsCompensating => CurrentState == SagaState.Compensating;

        public void TransitionTo(SagaState newState, string serviceName, string action, SagaEventType eventType, string? message = null, object? data = null)
        {
            var transition = new SagaStateTransition
            {
                SagaId = SagaId,
                FromState = CurrentState,
                ToState = newState,
                ServiceName = serviceName,
                Action = action,
                EventType = eventType,
                Message = message,
                Data = data
            };

            Transitions.Add(transition);
            CurrentState = newState;
            UpdatedAt = DateTime.UtcNow;

            if (IsCompleted)
            {
                CompletedAt = DateTime.UtcNow;
            }

            if (eventType == SagaEventType.Failure)
            {
                ErrorMessage = message;
            }
        }

        public SagaStateTransition? GetLastTransition()
        {
            return Transitions.OrderByDescending(t => t.Timestamp).FirstOrDefault();
        }

        public List<SagaStateTransition> GetTransitionsByService(string serviceName)
        {
            return Transitions.Where(t => t.ServiceName == serviceName).ToList();
        }

        public bool HasTransition(SagaState state)
        {
            return Transitions.Any(t => t.ToState == state);
        }
    }
}
