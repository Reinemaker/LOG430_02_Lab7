using System.Text.Json.Serialization;

namespace CornerShop.Shared.Events;

public class OrderSagaStartedEvent : BaseEvent
{
    public OrderSagaStartedEvent(string sagaId, string orderId, string customerId, decimal totalAmount, List<OrderItem> items)
    {
        EventType = "OrderSagaStarted";
        AggregateId = sagaId;
        AggregateType = "Saga";
        Data = new OrderSagaStartedData
        {
            SagaId = sagaId,
            OrderId = orderId,
            CustomerId = customerId,
            TotalAmount = totalAmount,
            Items = items,
            StartedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
        Metadata.Step = 1;
        Metadata.TotalSteps = 5;
    }

    public override object Data { get; }
}

public class OrderSagaStartedData
{
    [JsonPropertyName("sagaId")]
    public string SagaId { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("items")]
    public List<OrderItem> Items { get; set; } = new();

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }
}

public class OrderSagaStepCompletedEvent : BaseEvent
{
    public OrderSagaStepCompletedEvent(string sagaId, string stepName, int stepNumber, object stepData)
    {
        EventType = "OrderSagaStepCompleted";
        AggregateId = sagaId;
        AggregateType = "Saga";
        Data = new OrderSagaStepCompletedData
        {
            SagaId = sagaId,
            StepName = stepName,
            StepNumber = stepNumber,
            StepData = stepData,
            CompletedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
        Metadata.Step = stepNumber;
        Metadata.TotalSteps = 5;
    }

    public override object Data { get; }
}

public class OrderSagaStepCompletedData
{
    [JsonPropertyName("sagaId")]
    public string SagaId { get; set; } = string.Empty;

    [JsonPropertyName("stepName")]
    public string StepName { get; set; } = string.Empty;

    [JsonPropertyName("stepNumber")]
    public int StepNumber { get; set; }

    [JsonPropertyName("stepData")]
    public object StepData { get; set; } = new();

    [JsonPropertyName("completedAt")]
    public DateTime CompletedAt { get; set; }
}

public class OrderSagaStepFailedEvent : BaseEvent
{
    public OrderSagaStepFailedEvent(string sagaId, string stepName, int stepNumber, string errorMessage, object stepData)
    {
        EventType = "OrderSagaStepFailed";
        AggregateId = sagaId;
        AggregateType = "Saga";
        Data = new OrderSagaStepFailedData
        {
            SagaId = sagaId,
            StepName = stepName,
            StepNumber = stepNumber,
            ErrorMessage = errorMessage,
            StepData = stepData,
            FailedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
        Metadata.Step = stepNumber;
        Metadata.TotalSteps = 5;
    }

    public override object Data { get; }
}

public class OrderSagaStepFailedData
{
    [JsonPropertyName("sagaId")]
    public string SagaId { get; set; } = string.Empty;

    [JsonPropertyName("stepName")]
    public string StepName { get; set; } = string.Empty;

    [JsonPropertyName("stepNumber")]
    public int StepNumber { get; set; }

    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;

    [JsonPropertyName("stepData")]
    public object StepData { get; set; } = new();

    [JsonPropertyName("failedAt")]
    public DateTime FailedAt { get; set; }
}

public class OrderSagaCompensationStartedEvent : BaseEvent
{
    public OrderSagaCompensationStartedEvent(string sagaId, string failedStepName, int failedStepNumber)
    {
        EventType = "OrderSagaCompensationStarted";
        AggregateId = sagaId;
        AggregateType = "Saga";
        Data = new OrderSagaCompensationStartedData
        {
            SagaId = sagaId,
            FailedStepName = failedStepName,
            FailedStepNumber = failedStepNumber,
            CompensationStartedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
    }

    public override object Data { get; }
}

public class OrderSagaCompensationStartedData
{
    [JsonPropertyName("sagaId")]
    public string SagaId { get; set; } = string.Empty;

    [JsonPropertyName("failedStepName")]
    public string FailedStepName { get; set; } = string.Empty;

    [JsonPropertyName("failedStepNumber")]
    public int FailedStepNumber { get; set; }

    [JsonPropertyName("compensationStartedAt")]
    public DateTime CompensationStartedAt { get; set; }
}

public class OrderSagaCompensationCompletedEvent : BaseEvent
{
    public OrderSagaCompensationCompletedEvent(string sagaId, string stepName, int stepNumber)
    {
        EventType = "OrderSagaCompensationCompleted";
        AggregateId = sagaId;
        AggregateType = "Saga";
        Data = new OrderSagaCompensationCompletedData
        {
            SagaId = sagaId,
            StepName = stepName,
            StepNumber = stepNumber,
            CompensationCompletedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
    }

    public override object Data { get; }
}

public class OrderSagaCompensationCompletedData
{
    [JsonPropertyName("sagaId")]
    public string SagaId { get; set; } = string.Empty;

    [JsonPropertyName("stepName")]
    public string StepName { get; set; } = string.Empty;

    [JsonPropertyName("stepNumber")]
    public int StepNumber { get; set; }

    [JsonPropertyName("compensationCompletedAt")]
    public DateTime CompensationCompletedAt { get; set; }
}

public class OrderSagaCompletedEvent : BaseEvent
{
    public OrderSagaCompletedEvent(string sagaId, string orderId, decimal totalAmount)
    {
        EventType = "OrderSagaCompleted";
        AggregateId = sagaId;
        AggregateType = "Saga";
        Data = new OrderSagaCompletedData
        {
            SagaId = sagaId,
            OrderId = orderId,
            TotalAmount = totalAmount,
            CompletedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
    }

    public override object Data { get; }
}

public class OrderSagaCompletedData
{
    [JsonPropertyName("sagaId")]
    public string SagaId { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("completedAt")]
    public DateTime CompletedAt { get; set; }
}

public class OrderSagaFailedEvent : BaseEvent
{
    public OrderSagaFailedEvent(string sagaId, string orderId, string failureReason)
    {
        EventType = "OrderSagaFailed";
        AggregateId = sagaId;
        AggregateType = "Saga";
        Data = new OrderSagaFailedData
        {
            SagaId = sagaId,
            OrderId = orderId,
            FailureReason = failureReason,
            FailedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
    }

    public override object Data { get; }
}

public class OrderSagaFailedData
{
    [JsonPropertyName("sagaId")]
    public string SagaId { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("failureReason")]
    public string FailureReason { get; set; } = string.Empty;

    [JsonPropertyName("failedAt")]
    public DateTime FailedAt { get; set; }
}
