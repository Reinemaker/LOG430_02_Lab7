using Confluent.Kafka;
using CornerShop.Shared.Events;
using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using MongoDB.Driver;
using System.Text.Json;

namespace SagaOrchestrator.Services;

public interface ISagaOrchestratorService
{
    Task StartOrderSagaAsync(string orderId, string customerId, decimal totalAmount, List<OrderItem> items);
    Task HandleSagaStepCompletedAsync(string sagaId, string stepName, int stepNumber, object stepData);
    Task HandleSagaStepFailedAsync(string sagaId, string stepName, int stepNumber, string errorMessage, object stepData);
    Task HandleSagaCompensationCompletedAsync(string sagaId, string stepName, int stepNumber);
    Task<SagaState?> GetSagaStateAsync(string sagaId);
    Task<List<SagaState>> GetSagaStatesByOrderIdAsync(string orderId);
}

public class SagaOrchestratorService : ISagaOrchestratorService
{
    private readonly IMongoCollection<SagaState> _sagaStates;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<SagaOrchestratorService> _logger;

    public SagaOrchestratorService(IMongoDatabase database, IEventPublisher eventPublisher, ILogger<SagaOrchestratorService> logger)
    {
        _sagaStates = database.GetCollection<SagaState>("sagaStates");
        _eventPublisher = eventPublisher;
        _logger = logger;

        CreateIndexes();
    }

    private void CreateIndexes()
    {
        var sagaIdIndex = Builders<SagaState>.IndexKeys.Ascending(s => s.SagaId);
        var sagaIdIndexModel = new CreateIndexModel<SagaState>(sagaIdIndex, new CreateIndexOptions { Name = "SagaId", Unique = true });
        _sagaStates.Indexes.CreateOne(sagaIdIndexModel);

        var orderIdIndex = Builders<SagaState>.IndexKeys.Ascending(s => s.OrderId);
        var orderIdIndexModel = new CreateIndexModel<SagaState>(orderIdIndex, new CreateIndexOptions { Name = "OrderId" });
        _sagaStates.Indexes.CreateOne(orderIdIndexModel);

        var statusIndex = Builders<SagaState>.IndexKeys.Ascending(s => s.Status);
        var statusIndexModel = new CreateIndexModel<SagaState>(statusIndex, new CreateIndexOptions { Name = "Status" });
        _sagaStates.Indexes.CreateOne(statusIndexModel);
    }

    public async Task StartOrderSagaAsync(string orderId, string customerId, decimal totalAmount, List<OrderItem> items)
    {
        var sagaId = Guid.NewGuid().ToString();

        _logger.LogInformation("Starting Order Saga: {SagaId} for Order: {OrderId}", sagaId, orderId);

        // Create saga state
        var sagaState = new SagaState
        {
            SagaId = sagaId,
            OrderId = orderId,
            CustomerId = customerId,
            Status = SagaStatus.Started,
            CurrentStep = 1,
            TotalSteps = 5,
            StartedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Steps = new List<SagaStep>
            {
                new SagaStep { StepNumber = 1, StepName = SagaSteps.CreateOrder, Status = SagaStepStatus.Pending, StartedAt = DateTime.UtcNow },
                new SagaStep { StepNumber = 2, StepName = SagaSteps.ReserveStock, Status = SagaStepStatus.Pending, StartedAt = DateTime.UtcNow },
                new SagaStep { StepNumber = 3, StepName = SagaSteps.ProcessPayment, Status = SagaStepStatus.Pending, StartedAt = DateTime.UtcNow },
                new SagaStep { StepNumber = 4, StepName = SagaSteps.ConfirmOrder, Status = SagaStepStatus.Pending, StartedAt = DateTime.UtcNow },
                new SagaStep { StepNumber = 5, StepName = SagaSteps.SendNotifications, Status = SagaStepStatus.Pending, StartedAt = DateTime.UtcNow }
            }
        };

        await _sagaStates.InsertOneAsync(sagaState);

        // Publish saga started event
        var sagaStartedEvent = new OrderSagaStartedEvent(sagaId, orderId, customerId, totalAmount, items);
        await _eventPublisher.PublishAsync(sagaStartedEvent, "sagas.events");

        _logger.LogInformation("Order Saga started successfully: {SagaId}", sagaId);
    }

    public async Task HandleSagaStepCompletedAsync(string sagaId, string stepName, int stepNumber, object stepData)
    {
        _logger.LogInformation("Saga step completed: {SagaId} - {StepName} ({StepNumber})", sagaId, stepName, stepNumber);

        var filter = Builders<SagaState>.Filter.Eq(s => s.SagaId, sagaId);
        var update = Builders<SagaState>.Update
            .Set(s => s.CurrentStep, stepNumber + 1)
            .Set(s => s.UpdatedAt, DateTime.UtcNow)
            .Set($"steps.{stepNumber - 1}.status", SagaStepStatus.Completed)
            .Set($"steps.{stepNumber - 1}.completedAt", DateTime.UtcNow)
            .Set($"steps.{stepNumber - 1}.stepData", stepData);

        await _sagaStates.UpdateOneAsync(filter, update);

        // Publish step completed event
        var stepCompletedEvent = new OrderSagaStepCompletedEvent(sagaId, stepName, stepNumber, stepData);
        await _eventPublisher.PublishAsync(stepCompletedEvent, "sagas.events");

        // Check if saga is completed
        if (stepNumber == 5) // Last step
        {
            await CompleteSagaAsync(sagaId);
        }
        else
        {
            // Trigger next step
            await TriggerNextStepAsync(sagaId, stepNumber + 1);
        }
    }

    public async Task HandleSagaStepFailedAsync(string sagaId, string stepName, int stepNumber, string errorMessage, object stepData)
    {
        _logger.LogError("Saga step failed: {SagaId} - {StepName} ({StepNumber}): {ErrorMessage}", sagaId, stepName, stepNumber, errorMessage);

        var filter = Builders<SagaState>.Filter.Eq(s => s.SagaId, sagaId);
        var update = Builders<SagaState>.Update
            .Set(s => s.Status, SagaStatus.Compensating)
            .Set(s => s.FailedStep, stepNumber)
            .Set(s => s.FailureReason, errorMessage)
            .Set(s => s.UpdatedAt, DateTime.UtcNow)
            .Set($"steps.{stepNumber - 1}.status", SagaStepStatus.Failed)
            .Set($"steps.{stepNumber - 1}.failedAt", DateTime.UtcNow)
            .Set($"steps.{stepNumber - 1}.errorMessage", errorMessage)
            .Set($"steps.{stepNumber - 1}.stepData", stepData);

        await _sagaStates.UpdateOneAsync(filter, update);

        // Publish step failed event
        var stepFailedEvent = new OrderSagaStepFailedEvent(sagaId, stepName, stepNumber, errorMessage, stepData);
        await _eventPublisher.PublishAsync(stepFailedEvent, "sagas.events");

        // Start compensation
        await StartCompensationAsync(sagaId, stepNumber);
    }

    public async Task HandleSagaCompensationCompletedAsync(string sagaId, string stepName, int stepNumber)
    {
        _logger.LogInformation("Saga compensation completed: {SagaId} - {StepName} ({StepNumber})", sagaId, stepName, stepNumber);

        var filter = Builders<SagaState>.Filter.Eq(s => s.SagaId, sagaId);
        var update = Builders<SagaState>.Update
            .Set($"steps.{stepNumber - 1}.status", SagaStepStatus.Compensated)
            .Set($"steps.{stepNumber - 1}.compensatedAt", DateTime.UtcNow)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        await _sagaStates.UpdateOneAsync(filter, update);

        // Publish compensation completed event
        var compensationCompletedEvent = new OrderSagaCompensationCompletedEvent(sagaId, stepName, stepNumber);
        await _eventPublisher.PublishAsync(compensationCompletedEvent, "sagas.events");

        // Check if all compensations are completed
        var sagaState = await GetSagaStateAsync(sagaId);
        if (sagaState != null && sagaState.Steps.All(s => s.Status == SagaStepStatus.Compensated || s.Status == SagaStepStatus.Pending))
        {
            await FailSagaAsync(sagaId, sagaState.FailureReason ?? "Unknown error");
        }
    }

    public async Task<SagaState?> GetSagaStateAsync(string sagaId)
    {
        var filter = Builders<SagaState>.Filter.Eq(s => s.SagaId, sagaId);
        return await _sagaStates.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<SagaState>> GetSagaStatesByOrderIdAsync(string orderId)
    {
        var filter = Builders<SagaState>.Filter.Eq(s => s.OrderId, orderId);
        return await _sagaStates.Find(filter).ToListAsync();
    }

    private async Task CompleteSagaAsync(string sagaId)
    {
        _logger.LogInformation("Completing Order Saga: {SagaId}", sagaId);

        var sagaState = await GetSagaStateAsync(sagaId);
        if (sagaState == null) return;

        var filter = Builders<SagaState>.Filter.Eq(s => s.SagaId, sagaId);
        var update = Builders<SagaState>.Update
            .Set(s => s.Status, SagaStatus.Completed)
            .Set(s => s.CompletedAt, DateTime.UtcNow)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        await _sagaStates.UpdateOneAsync(filter, update);

        // Publish saga completed event
        var sagaCompletedEvent = new OrderSagaCompletedEvent(sagaId, sagaState.OrderId, sagaState.TotalAmount);
        await _eventPublisher.PublishAsync(sagaCompletedEvent, "sagas.events");

        _logger.LogInformation("Order Saga completed successfully: {SagaId}", sagaId);
    }

    private async Task FailSagaAsync(string sagaId, string failureReason)
    {
        _logger.LogError("Failing Order Saga: {SagaId} - {FailureReason}", sagaId, failureReason);

        var sagaState = await GetSagaStateAsync(sagaId);
        if (sagaState == null) return;

        var filter = Builders<SagaState>.Filter.Eq(s => s.SagaId, sagaId);
        var update = Builders<SagaState>.Update
            .Set(s => s.Status, SagaStatus.Failed)
            .Set(s => s.FailedAt, DateTime.UtcNow)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        await _sagaStates.UpdateOneAsync(filter, update);

        // Publish saga failed event
        var sagaFailedEvent = new OrderSagaFailedEvent(sagaId, sagaState.OrderId, failureReason);
        await _eventPublisher.PublishAsync(sagaFailedEvent, "sagas.events");

        _logger.LogError("Order Saga failed: {SagaId}", sagaId);
    }

    private async Task StartCompensationAsync(string sagaId, int failedStepNumber)
    {
        _logger.LogInformation("Starting compensation for Saga: {SagaId}, failed step: {StepNumber}", sagaId, failedStepNumber);

        // Publish compensation started event
        var sagaState = await GetSagaStateAsync(sagaId);
        if (sagaState == null) return;

        var failedStep = sagaState.Steps.FirstOrDefault(s => s.StepNumber == failedStepNumber);
        if (failedStep == null) return;

        var compensationStartedEvent = new OrderSagaCompensationStartedEvent(sagaId, failedStep.StepName, failedStepNumber);
        await _eventPublisher.PublishAsync(compensationStartedEvent, "sagas.events");

        // Trigger compensation for all completed steps in reverse order
        for (int i = failedStepNumber - 1; i >= 1; i--)
        {
            var step = sagaState.Steps.FirstOrDefault(s => s.StepNumber == i);
            if (step?.Status == SagaStepStatus.Completed)
            {
                await TriggerCompensationAsync(sagaId, i);
            }
        }
    }

    private async Task TriggerNextStepAsync(string sagaId, int nextStepNumber)
    {
        _logger.LogInformation("Triggering next step for Saga: {SagaId}, step: {StepNumber}", sagaId, nextStepNumber);

        // This would trigger the next step in the saga
        // In a choreographed saga, each service listens for events and triggers the next step
        // Here we just log the trigger
        _logger.LogInformation("Next step triggered: {SagaId} - Step {StepNumber}", sagaId, nextStepNumber);
    }

    private async Task TriggerCompensationAsync(string sagaId, int stepNumber)
    {
        _logger.LogInformation("Triggering compensation for Saga: {SagaId}, step: {StepNumber}", sagaId, stepNumber);

        // This would trigger compensation for the specific step
        // In a choreographed saga, each service listens for compensation events
        // Here we just log the compensation trigger
        _logger.LogInformation("Compensation triggered: {SagaId} - Step {StepNumber}", sagaId, stepNumber);
    }
}
