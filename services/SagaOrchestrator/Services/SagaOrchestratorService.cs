using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using System.Text.Json;
using Prometheus;

namespace SagaOrchestrator.Services
{
    /// <summary>
    /// Saga Orchestrator Service - Coordinates distributed saga execution
    /// </summary>
    public class SagaOrchestratorService : ISagaOrchestrator
    {
        private readonly ISagaStateManager _stateManager;
        private readonly IEventProducer _eventProducer;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SagaOrchestratorService> _logger;
        private readonly Dictionary<string, string> _serviceEndpoints;

        // Prometheus metrics
        private static readonly Counter SagaExecutionsTotal = Metrics.CreateCounter("saga_executions_total", "Total number of saga executions", new CounterConfiguration
        {
            LabelNames = new[] { "saga_type", "result" }
        });

        private static readonly Histogram SagaExecutionDuration = Metrics.CreateHistogram("saga_execution_duration_seconds", "Saga execution duration in seconds", new HistogramConfiguration
        {
            LabelNames = new[] { "saga_type" },
            Buckets = new[] { 0.1, 0.5, 1.0, 2.0, 5.0, 10.0, 30.0, 60.0 }
        });

        private static readonly Counter SagaStepExecutionsTotal = Metrics.CreateCounter("saga_step_executions_total", "Total number of saga step executions", new CounterConfiguration
        {
            LabelNames = new[] { "step_name", "service_name", "result" }
        });

        private static readonly Histogram SagaStepExecutionDuration = Metrics.CreateHistogram("saga_step_execution_duration_seconds", "Saga step execution duration in seconds", new HistogramConfiguration
        {
            LabelNames = new[] { "step_name", "service_name" },
            Buckets = new[] { 0.01, 0.05, 0.1, 0.5, 1.0, 2.0, 5.0 }
        });

        private static readonly Counter SagaCompensationsTotal = Metrics.CreateCounter("saga_compensations_total", "Total number of saga compensations", new CounterConfiguration
        {
            LabelNames = new[] { "saga_type", "reason" }
        });

        public SagaOrchestratorService(
            ISagaStateManager stateManager,
            IEventProducer eventProducer,
            IHttpClientFactory httpClientFactory,
            ILogger<SagaOrchestratorService> logger)
        {
            _stateManager = stateManager;
            _eventProducer = eventProducer;
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            // Configure microservice endpoints
            _serviceEndpoints = new Dictionary<string, string>
            {
                { "StockService", "http://stock-service:80" },
                { "PaymentService", "http://payment-service:80" },
                { "OrderService", "http://order-service:80" }
            };
        }

        public async Task<SagaOrchestrationResponse> ExecuteSagaAsync(SagaOrchestrationRequest request)
        {
            var sagaId = request.SagaId;
            var correlationId = request.CorrelationId ?? Guid.NewGuid().ToString();
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("Starting saga execution: {SagaId} | {SagaType} | {OrderId}", 
                sagaId, request.SagaType, request.OrderId);

            try
            {
                // Create saga state
                await _stateManager.CreateSagaAsync(request.SagaType, request.OrderId, correlationId);

                // Publish saga started event
                var sagaStartedEvent = new SagaStartedEvent
                {
                    SagaId = sagaId,
                    SagaType = request.SagaType,
                    OrderId = request.OrderId,
                    CorrelationId = correlationId
                };
                await _eventProducer.PublishSagaEventAsync(sagaStartedEvent, correlationId);

                // Define saga steps
                var steps = new List<SagaStep>
                {
                    new SagaStep { StepName = "VerifyStock", ServiceName = "StockService", Status = "Pending" },
                    new SagaStep { StepName = "ReserveStock", ServiceName = "StockService", Status = "Pending", CompensationRequired = true },
                    new SagaStep { StepName = "ProcessPayment", ServiceName = "PaymentService", Status = "Pending", CompensationRequired = true },
                    new SagaStep { StepName = "ConfirmOrder", ServiceName = "OrderService", Status = "Pending" }
                };

                var response = new SagaOrchestrationResponse
                {
                    SagaId = sagaId,
                    OrderId = request.OrderId,
                    Status = "InProgress",
                    CurrentState = SagaState.Started,
                    Steps = steps,
                    StartedAt = DateTime.UtcNow
                };

                // Execute saga steps
                await ExecuteSagaStepsAsync(request, steps, correlationId);

                // Update final state
                response.Status = "Success";
                response.CurrentState = SagaState.Completed;
                response.CompletedAt = DateTime.UtcNow;

                await _stateManager.CompleteSagaAsync(sagaId, "Success");

                // Publish saga completed event
                var sagaCompletedEvent = new SagaCompletedEvent
                {
                    SagaId = sagaId,
                    SagaType = request.SagaType,
                    Result = "Success",
                    CorrelationId = correlationId
                };
                await _eventProducer.PublishSagaEventAsync(sagaCompletedEvent, correlationId);

                // Record metrics
                var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                SagaExecutionsTotal.WithLabels(request.SagaType, "success").Inc();
                SagaExecutionDuration.WithLabels(request.SagaType).Observe(duration);

                _logger.LogInformation("Saga completed successfully: {SagaId} | Duration: {Duration}", 
                    sagaId, response.Duration);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saga execution failed: {SagaId}", sagaId);

                // Record failure metrics
                var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                SagaExecutionsTotal.WithLabels(request.SagaType, "failure").Inc();
                SagaExecutionDuration.WithLabels(request.SagaType).Observe(duration);

                // Update state to failed
                await _stateManager.UpdateSagaStateAsync(sagaId, SagaState.Failed, "SagaOrchestrator", "ExecuteSaga", SagaEventType.Failure, ex.Message);

                // Attempt compensation
                await CompensateSagaAsync(sagaId, ex.Message);

                return new SagaOrchestrationResponse
                {
                    SagaId = sagaId,
                    OrderId = request.OrderId,
                    Status = "Failed",
                    CurrentState = SagaState.Failed,
                    ErrorMessage = ex.Message,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };
            }
        }

        private async Task ExecuteSagaStepsAsync(SagaOrchestrationRequest request, List<SagaStep> steps, string correlationId)
        {
            var sagaId = request.SagaId;

            foreach (var step in steps)
            {
                try
                {
                    _logger.LogInformation("Executing saga step: {SagaId} | {StepName} | {ServiceName}", 
                        sagaId, step.StepName, step.ServiceName);

                    // Update step status
                    step.Status = "InProgress";
                    step.StartedAt = DateTime.UtcNow;

                    // Update saga state
                    var newState = GetSagaStateForStep(step.StepName);
                    await _stateManager.UpdateSagaStateAsync(sagaId, newState, step.ServiceName, step.StepName, SagaEventType.Success);

                    // Execute step
                    var stepRequest = new SagaParticipantRequest
                    {
                        SagaId = sagaId,
                        StepName = step.StepName,
                        OrderId = request.OrderId,
                        Data = GetStepData(request, step.StepName),
                        CorrelationId = correlationId
                    };

                    var stepStartTime = DateTime.UtcNow;
                    var stepResponse = await ExecuteStepAsync(step.ServiceName, stepRequest);
                    var stepDuration = (DateTime.UtcNow - stepStartTime).TotalSeconds;

                    if (stepResponse.Success)
                    {
                        step.Status = "Completed";
                        step.CompletedAt = DateTime.UtcNow;
                        
                        // Record step success metrics
                        SagaStepExecutionsTotal.WithLabels(step.StepName, step.ServiceName, "success").Inc();
                        SagaStepExecutionDuration.WithLabels(step.StepName, step.ServiceName).Observe(stepDuration);
                        
                        _logger.LogInformation("Saga step completed: {SagaId} | {StepName}", sagaId, step.StepName);
                    }
                    else
                    {
                        step.Status = "Failed";
                        step.ErrorMessage = stepResponse.ErrorMessage;
                        step.CompletedAt = DateTime.UtcNow;
                        
                        // Record step failure metrics
                        SagaStepExecutionsTotal.WithLabels(step.StepName, step.ServiceName, "failure").Inc();
                        SagaStepExecutionDuration.WithLabels(step.StepName, step.ServiceName).Observe(stepDuration);
                        
                        throw new Exception($"Step {step.StepName} failed: {stepResponse.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    step.Status = "Failed";
                    step.ErrorMessage = ex.Message;
                    step.CompletedAt = DateTime.UtcNow;

                    _logger.LogError(ex, "Saga step failed: {SagaId} | {StepName}", sagaId, step.StepName);

                    // Update saga state to failed
                    await _stateManager.UpdateSagaStateAsync(sagaId, SagaState.Failed, step.ServiceName, step.StepName, SagaEventType.Failure, ex.Message);

                    throw;
                }
            }
        }

        private async Task<SagaParticipantResponse> ExecuteStepAsync(string serviceName, SagaParticipantRequest request)
        {
            if (!_serviceEndpoints.ContainsKey(serviceName))
            {
                throw new Exception($"Service endpoint not found for: {serviceName}");
            }

            var endpoint = _serviceEndpoints[serviceName];
            var client = _httpClientFactory.CreateClient("Microservices");

            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{endpoint}/api/saga/participate", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<SagaParticipantResponse>(responseContent) ?? new SagaParticipantResponse { Success = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute step: {ServiceName} | {StepName}", serviceName, request.StepName);
                return new SagaParticipantResponse
                {
                    SagaId = request.SagaId,
                    StepName = request.StepName,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private object? GetStepData(SagaOrchestrationRequest request, string stepName)
        {
            return stepName switch
            {
                "VerifyStock" => new { Items = request.Items },
                "ReserveStock" => new { Items = request.Items },
                "ProcessPayment" => new { Amount = request.TotalAmount, PaymentMethod = request.PaymentMethod },
                "ConfirmOrder" => new { OrderId = request.OrderId, CustomerId = request.CustomerId, StoreId = request.StoreId },
                _ => null
            };
        }

        private SagaState GetSagaStateForStep(string stepName)
        {
            return stepName switch
            {
                "VerifyStock" => SagaState.StockVerifying,
                "ReserveStock" => SagaState.StockReserving,
                "ProcessPayment" => SagaState.PaymentProcessing,
                "ConfirmOrder" => SagaState.OrderConfirming,
                _ => SagaState.Started
            };
        }

        public async Task<SagaOrchestrationResponse> GetSagaStatusAsync(string sagaId)
        {
            var state = await _stateManager.GetSagaStateAsync(sagaId);
            var transitions = await _stateManager.GetSagaTransitionsAsync(sagaId);

            return new SagaOrchestrationResponse
            {
                SagaId = sagaId,
                Status = state.ToString(),
                CurrentState = state,
                Steps = new List<SagaStep>(), // Would need to reconstruct from transitions
                StartedAt = transitions.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow
            };
        }

        public async Task<SagaOrchestrationResponse> CompensateSagaAsync(string sagaId, string reason)
        {
            _logger.LogInformation("Starting saga compensation: {SagaId} | Reason: {Reason}", sagaId, reason);

            // Record compensation metrics
            SagaCompensationsTotal.WithLabels("OrderCreation", reason).Inc();

            try
            {
                await _stateManager.UpdateSagaStateAsync(sagaId, SagaState.Compensating, "SagaOrchestrator", "CompensateSaga", SagaEventType.Compensation, reason);

                // Get saga transitions to determine which steps need compensation
                var transitions = await _stateManager.GetSagaTransitionsAsync(sagaId);
                var compensatedSteps = new List<string>();

                // Compensate steps in reverse order
                var stepsToCompensate = transitions
                    .Where(t => t.EventType == SagaEventType.Success)
                    .OrderByDescending(t => t.Timestamp)
                    .ToList();

                foreach (var transition in stepsToCompensate)
                {
                    try
                    {
                        var compensationRequest = new SagaCompensationRequest
                        {
                            SagaId = sagaId,
                            StepName = transition.Action,
                            OrderId = "", // Would need to extract from data
                            Reason = reason
                        };

                        var compensationResponse = await CompensateStepAsync(transition.ServiceName, compensationRequest);
                        
                        if (compensationResponse.Success)
                        {
                            compensatedSteps.Add(transition.Action);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Compensation failed for step: {SagaId} | {StepName}", sagaId, transition.Action);
                    }
                }

                await _stateManager.UpdateSagaStateAsync(sagaId, SagaState.Compensated, "SagaOrchestrator", "CompensateSaga", SagaEventType.Compensation, "Compensation completed");

                // Publish saga compensated event
                var sagaCompensatedEvent = new SagaCompensatedEvent
                {
                    SagaId = sagaId,
                    SagaType = "OrderCreation",
                    CompensationReason = reason,
                    CompensatedSteps = compensatedSteps
                };
                await _eventProducer.PublishSagaEventAsync(sagaCompensatedEvent);

                return new SagaOrchestrationResponse
                {
                    SagaId = sagaId,
                    Status = "Compensated",
                    CurrentState = SagaState.Compensated,
                    CompletedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saga compensation failed: {SagaId}", sagaId);
                throw;
            }
        }

        private async Task<SagaCompensationResponse> CompensateStepAsync(string serviceName, SagaCompensationRequest request)
        {
            if (!_serviceEndpoints.ContainsKey(serviceName))
            {
                throw new Exception($"Service endpoint not found for: {serviceName}");
            }

            var endpoint = _serviceEndpoints[serviceName];
            var client = _httpClientFactory.CreateClient("Microservices");

            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{endpoint}/api/saga/compensate", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<SagaCompensationResponse>(responseContent) ?? new SagaCompensationResponse { Success = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compensate step: {ServiceName} | {StepName}", serviceName, request.StepName);
                return new SagaCompensationResponse
                {
                    SagaId = request.SagaId,
                    StepName = request.StepName,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<SagaMetrics> GetSagaMetricsAsync()
        {
            // This would typically query a metrics store
            // For now, return basic metrics
            return new SagaMetrics
            {
                TotalSagas = 0,
                SuccessfulSagas = 0,
                FailedSagas = 0,
                CompensatedSagas = 0,
                AverageDuration = TimeSpan.Zero,
                SagasByState = new Dictionary<SagaState, int>(),
                SagasByType = new Dictionary<string, int>()
            };
        }
    }
} 