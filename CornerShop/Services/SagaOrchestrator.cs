using CornerShop.Models;
using System.Text.Json;

namespace CornerShop.Services
{
    public class SagaOrchestrator : ISagaOrchestrator
    {
        private readonly IDatabaseService _databaseService;
        private readonly IProductService _productService;
        private readonly ISaleService _saleService;
        private readonly IStoreService _storeService;
        private readonly ISagaStateManager _stateManager;
        private readonly ISagaEventPublisher _eventPublisher;
        private readonly IControlledFailureService _failureService;
        private readonly ISagaMetricsService _metricsService;
        private readonly IBusinessEventLogger _businessLogger;
        private readonly ILogger<SagaOrchestrator> _logger;
        private readonly Dictionary<string, SagaResult> _activeSagas = new();

        public SagaOrchestrator(
            IDatabaseService databaseService,
            IProductService productService,
            ISaleService saleService,
            IStoreService storeService,
            ISagaStateManager stateManager,
            ISagaEventPublisher eventPublisher,
            IControlledFailureService failureService,
            ISagaMetricsService metricsService,
            IBusinessEventLogger businessLogger,
            ILogger<SagaOrchestrator> logger)
        {
            _databaseService = databaseService;
            _productService = productService;
            _saleService = saleService;
            _storeService = storeService;
            _stateManager = stateManager;
            _eventPublisher = eventPublisher;
            _failureService = failureService;
            _metricsService = metricsService;
            _businessLogger = businessLogger;
            _logger = logger;
        }

        public async Task<SagaResult> ExecuteSaleSagaAsync(CreateSaleRequest saleRequest)
        {
            var sagaId = Guid.NewGuid().ToString();
            var saga = new SagaResult
            {
                SagaId = sagaId,
                IsSuccess = false,
                Steps = new List<SagaStep>()
            };

            _activeSagas[sagaId] = saga;

            // Create and initialize state machine
            var stateMachine = await _stateManager.CreateSagaAsync(sagaId, "SaleSaga");

            // Record metrics and structured logging
            _metricsService.RecordSagaStart("SaleSaga", sagaId);
            _businessLogger.LogSagaLifecycle("started", sagaId, "SaleSaga", new Dictionary<string, object>
            {
                ["store_id"] = saleRequest.StoreId,
                ["item_count"] = saleRequest.Items.Count,
                ["total_items"] = saleRequest.Items.Sum(i => i.Quantity)
            });

            _logger.LogInformation("Starting Sale Saga {SagaId} for store {StoreId}", sagaId, saleRequest.StoreId);

            try
            {
                // Step 1: Validate store exists
                var step1 = await ExecuteStepAsync(sagaId, "StoreService", "ValidateStore", async () =>
                {
                    var store = await _storeService.GetStoreById(saleRequest.StoreId, sagaId) ?? throw new InvalidOperationException($"Store {saleRequest.StoreId} not found");

                    // Update state machine
                    await _stateManager.UpdateSagaStateAsync(sagaId, SagaState.StoreValidated, "StoreService", "ValidateStore", SagaEventType.Success, $"Store {store.Name} validated");

                    return store;
                });

                // Step 2: Validate and reserve stock
                var step2 = await ExecuteStepAsync(sagaId, "ProductService", "ValidateAndReserveStock", async () =>
                {
                    var stockValidationResults = new List<object>();
                    foreach (var item in saleRequest.Items)
                    {
                        if (!await _productService.ValidateProductExists(item.ProductName, saleRequest.StoreId, sagaId))
                            throw new InvalidOperationException($"Product {item.ProductName} not found in store {saleRequest.StoreId}");

                        if (!await _productService.ValidateStockAvailability(item.ProductName, saleRequest.StoreId, item.Quantity, sagaId))
                            throw new InvalidOperationException($"Insufficient stock for {item.ProductName}");

                        // Reserve stock (temporary hold)
                        await _productService.UpdateStock(item.ProductName, saleRequest.StoreId, -item.Quantity, sagaId);
                        stockValidationResults.Add(new { item.ProductName, ReservedQuantity = item.Quantity });
                    }

                    // Update state machine
                    await _stateManager.UpdateSagaStateAsync(sagaId, SagaState.StockReserved, "ProductService", "ValidateAndReserveStock", SagaEventType.Success, $"Stock reserved for {saleRequest.Items.Count} items");

                    return stockValidationResults;
                }, async () =>
                {
                    // Compensation: Release reserved stock
                    await _stateManager.UpdateSagaStateAsync(sagaId, SagaState.Compensating, "SagaOrchestrator", "CompensateStock", SagaEventType.Success, "Releasing reserved stock");

                    foreach (var item in saleRequest.Items)
                    {
                        await _productService.UpdateStock(item.ProductName, saleRequest.StoreId, item.Quantity, sagaId);
                    }

                    await _stateManager.UpdateSagaStateAsync(sagaId, SagaState.Compensated, "SagaOrchestrator", "CompensateStock", SagaEventType.Success, "Stock compensation completed");
                });

                // Step 3: Calculate sale total
                var step3 = await ExecuteStepAsync(sagaId, "SaleService", "CalculateTotal", async () =>
                {
                    var saleItems = saleRequest.Items.Select(item => new SaleItem
                    {
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        Price = item.UnitPrice
                    }).ToList();

                    var total = await _saleService.CalculateSaleTotal(saleItems, saleRequest.StoreId, sagaId);

                    // Update state machine
                    await _stateManager.UpdateSagaStateAsync(sagaId, SagaState.TotalCalculated, "SaleService", "CalculateTotal", SagaEventType.Success, $"Sale total calculated: {total:C}");

                    return total;
                });

                // Step 4: Create sale record
                Sale? createdSale = null;
                var step4 = await ExecuteStepAsync(sagaId, "SaleService", "CreateSale", async () =>
                {
                    var saleItems = saleRequest.Items.Select(item => new SaleItem
                    {
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        Price = item.UnitPrice
                    }).ToList();

                    var sale = new Sale
                    {
                        StoreId = saleRequest.StoreId,
                        Date = DateTime.UtcNow,
                        Items = saleItems,
                        TotalAmount = (decimal)step3.Data!,
                        Status = "Completed"
                    };

                    var saleId = await _saleService.CreateSale(sale, sagaId);
                    sale.Id = saleId;
                    createdSale = sale;

                    // Update state machine
                    await _stateManager.UpdateSagaStateAsync(sagaId, SagaState.SaleCreated, "SaleService", "CreateSale", SagaEventType.Success, $"Sale created with ID {saleId}");

                    return sale;
                }, async () =>
                {
                    // Compensation: Cancel sale if created
                    await _stateManager.UpdateSagaStateAsync(sagaId, SagaState.Compensating, "SagaOrchestrator", "CompensateSale", SagaEventType.Success, "Cancelling created sale");

                    if (createdSale != null && !string.IsNullOrEmpty(createdSale.Id))
                    {
                        await _saleService.CancelSale(createdSale.Id, createdSale.StoreId);
                    }

                    await _stateManager.UpdateSagaStateAsync(sagaId, SagaState.Compensated, "SagaOrchestrator", "CompensateSale", SagaEventType.Success, "Sale compensation completed");
                });

                // Step 5: Update inventory (confirm stock reduction)
                var step5 = await ExecuteStepAsync(sagaId, "ProductService", "ConfirmStockReduction", async () =>
                {
                    // Stock was already reduced in step 2, this step confirms the reduction
                    // and could trigger reorder alerts if needed
                    var lowStockProducts = new List<string>();
                    foreach (var item in saleRequest.Items)
                    {
                        var product = await _productService.GetProductByName(item.ProductName, saleRequest.StoreId);
                        if (product != null && product.StockQuantity <= product.ReorderPoint)
                        {
                            lowStockProducts.Add(item.ProductName);
                        }
                    }

                    // Update state machine to completed
                    await _stateManager.UpdateSagaStateAsync(sagaId, SagaState.Completed, "ProductService", "ConfirmStockReduction", SagaEventType.Success, $"Saga completed. Low stock products: {lowStockProducts.Count}");

                    return lowStockProducts;
                });

                saga.IsSuccess = true;
                saga.CompletedAt = DateTime.UtcNow;

                // Record metrics and structured logging
                var duration = DateTime.UtcNow - saga.CreatedAt;
                _metricsService.RecordSagaCompletion("SaleSaga", sagaId, true, duration);
                _businessLogger.LogSagaLifecycle("completed", sagaId, "SaleSaga", new Dictionary<string, object>
                {
                    ["duration_seconds"] = duration.TotalSeconds,
                    ["steps_completed"] = saga.Steps.Count,
                    ["total_amount"] = createdSale?.TotalAmount ?? 0
                });

                _logger.LogInformation("Sale Saga {SagaId} completed successfully", sagaId);

                return saga;
            }
            catch (Exception ex)
            {
                saga.ErrorMessage = ex.Message;

                // Record metrics and structured logging
                var duration = DateTime.UtcNow - saga.CreatedAt;
                _metricsService.RecordSagaCompletion("SaleSaga", sagaId, false, duration);
                _businessLogger.LogSagaLifecycle("failed", sagaId, "SaleSaga", new Dictionary<string, object>
                {
                    ["duration_seconds"] = duration.TotalSeconds,
                    ["steps_completed"] = saga.Steps.Count,
                    ["error_message"] = ex.Message,
                    ["error_type"] = ex.GetType().Name
                });

                _logger.LogError(ex, "Sale Saga {SagaId} failed: {ErrorMessage}", sagaId, ex.Message);

                // Update state machine to failed
                await _stateManager.UpdateSagaStateAsync(sagaId, SagaState.Failed, "SagaOrchestrator", "SagaFailed", SagaEventType.Failure, ex.Message);

                // Attempt compensation
                await CompensateSagaAsync(sagaId);

                return saga;
            }
            finally
            {
                _activeSagas.Remove(sagaId);
            }
        }

        public async Task<SagaResult> ExecuteOrderSagaAsync(CreateOrderRequest orderRequest)
        {
            var sagaId = Guid.NewGuid().ToString();
            var saga = new SagaResult
            {
                SagaId = sagaId,
                IsSuccess = false,
                Steps = new List<SagaStep>()
            };

            _activeSagas[sagaId] = saga;
            _logger.LogInformation("Starting Order Saga {SagaId} for customer {CustomerId}", sagaId, orderRequest.CustomerId);

            try
            {
                // Step 1: Validate customer
                var step1 = await ExecuteStepAsync(sagaId, "CustomerService", "ValidateCustomer", async () =>
                {
                    // This would call the customer service
                    // For now, we'll simulate it
                    if (string.IsNullOrEmpty(orderRequest.CustomerId))
                        throw new InvalidOperationException("Customer ID is required");
                    return new { orderRequest.CustomerId, IsValid = true };
                });

                // Step 2: Create cart
                var step2 = await ExecuteStepAsync(sagaId, "CartService", "CreateCart", async () =>
                {
                    // This would call the cart service
                    var cartId = Guid.NewGuid().ToString();
                    return new { CartId = cartId, orderRequest.Items };
                });

                // Step 3: Process payment
                var step3 = await ExecuteStepAsync(sagaId, "PaymentService", "ProcessPayment", async () =>
                {
                    // This would call the payment service
                    var paymentId = Guid.NewGuid().ToString();
                    return new { PaymentId = paymentId, Amount = orderRequest.Items.Sum(i => i.Quantity * i.UnitPrice) };
                });

                // Step 4: Create order
                var step4 = await ExecuteStepAsync(sagaId, "OrderService", "CreateOrder", async () =>
                {
                    // This would call the order service
                    var orderId = Guid.NewGuid().ToString();
                    return new { OrderId = orderId, Status = "Confirmed" };
                });

                saga.IsSuccess = true;
                saga.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation("Order Saga {SagaId} completed successfully", sagaId);

                return saga;
            }
            catch (Exception ex)
            {
                saga.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Order Saga {SagaId} failed: {ErrorMessage}", sagaId, ex.Message);

                await CompensateSagaAsync(sagaId);
                return saga;
            }
            finally
            {
                _activeSagas.Remove(sagaId);
            }
        }

        public async Task<SagaResult> ExecuteStockUpdateSagaAsync(StockUpdateRequest stockRequest)
        {
            var sagaId = Guid.NewGuid().ToString();
            var saga = new SagaResult
            {
                SagaId = sagaId,
                IsSuccess = false,
                Steps = new List<SagaStep>()
            };

            _activeSagas[sagaId] = saga;
            _logger.LogInformation("Starting Stock Update Saga {SagaId} for product {ProductName}", sagaId, stockRequest.ProductName);

            try
            {
                // Step 1: Validate product exists
                var step1 = await ExecuteStepAsync(sagaId, "ProductService", "ValidateProduct", async () =>
                {
                    if (!await _productService.ValidateProductExists(stockRequest.ProductName, stockRequest.StoreId))
                        throw new InvalidOperationException($"Product {stockRequest.ProductName} not found");
                    return new { stockRequest.ProductName, stockRequest.StoreId };
                });

                // Step 2: Update stock
                var step2 = await ExecuteStepAsync(sagaId, "ProductService", "UpdateStock", async () =>
                {
                    var oldStock = await _productService.GetProductByName(stockRequest.ProductName, stockRequest.StoreId);
                    await _productService.UpdateStock(stockRequest.ProductName, stockRequest.StoreId, stockRequest.Quantity);
                    return new { OldStock = oldStock?.StockQuantity ?? 0, NewStock = (oldStock?.StockQuantity ?? 0) + stockRequest.Quantity };
                });

                // Step 3: Update reporting data
                var step3 = await ExecuteStepAsync(sagaId, "ReportingService", "UpdateStockReport", async () =>
                {
                    // This would update reporting data
                    return new { Updated = true, Timestamp = DateTime.UtcNow };
                });

                saga.IsSuccess = true;
                saga.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation("Stock Update Saga {SagaId} completed successfully", sagaId);

                return saga;
            }
            catch (Exception ex)
            {
                saga.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Stock Update Saga {SagaId} failed: {ErrorMessage}", sagaId, ex.Message);

                await CompensateSagaAsync(sagaId);
                return saga;
            }
            finally
            {
                _activeSagas.Remove(sagaId);
            }
        }

        public async Task<SagaResult> CompensateSagaAsync(string sagaId)
        {
            if (!_activeSagas.TryGetValue(sagaId, out var saga))
            {
                _logger.LogWarning("Saga {SagaId} not found for compensation", sagaId);
                return new SagaResult { IsSuccess = false, ErrorMessage = "Saga not found" };
            }

            _logger.LogInformation("Starting enhanced compensation for Saga {SagaId}", sagaId);

            // Update state machine to compensating
            await _stateManager.UpdateSagaStateAsync(sagaId, SagaState.Compensating, "SagaOrchestrator", "StartCompensation", SagaEventType.Success, "Starting saga compensation");

            // Execute compensation steps in reverse order
            var completedSteps = saga.Steps.Where(s => s.IsCompleted && !s.IsCompensated).Reverse().ToList();

            var compensationResults = new List<CompensationResult>();

            foreach (var step in completedSteps)
            {
                var compensationResult = new CompensationResult
                {
                    StepId = step.StepId,
                    ServiceName = step.ServiceName,
                    Action = step.Action,
                    StartedAt = DateTime.UtcNow
                };

                try
                {
                    if (step.CompensationAction != null)
                    {
                        // Simulate controlled failure during compensation
                        await _failureService.SimulateNetworkTimeoutAsync($"{step.ServiceName}_Compensation", sagaId);

                        var compensationStartTime = DateTime.UtcNow;
                        await step.CompensationAction();
                        var compensationDuration = DateTime.UtcNow - compensationStartTime;

                        step.IsCompensated = true;
                        compensationResult.IsSuccessful = true;
                        compensationResult.CompletedAt = DateTime.UtcNow;

                        // Record metrics and structured logging
                        _metricsService.RecordCompensation("SaleSaga", step.Action, step.ServiceName, true, compensationDuration);
                        _businessLogger.LogCompensation(sagaId, "SaleSaga", step.Action, step.ServiceName, true, new Dictionary<string, object>
                        {
                            ["duration_seconds"] = compensationDuration.TotalSeconds,
                            ["step_id"] = step.StepId
                        });

                        _logger.LogInformation("Successfully compensated step {StepId} in saga {SagaId}", step.StepId, sagaId);

                        // Publish compensation success event
                        await _eventPublisher.PublishSagaEventAsync(sagaId, "SagaOrchestrator", "CompensationSuccess", SagaEventType.Success, $"Step {step.StepId} compensated successfully", new { step.StepId, step.ServiceName, step.Action });
                    }
                    else
                    {
                        compensationResult.IsSuccessful = false;
                        compensationResult.ErrorMessage = "No compensation action defined";

                        // Record metrics and structured logging
                        _metricsService.RecordCompensation("SaleSaga", step.Action, step.ServiceName, false, TimeSpan.Zero);
                        _businessLogger.LogCompensation(sagaId, "SaleSaga", step.Action, step.ServiceName, false, new Dictionary<string, object>
                        {
                            ["error_message"] = "No compensation action defined",
                            ["step_id"] = step.StepId
                        });

                        _logger.LogWarning("No compensation action defined for step {StepId} in saga {SagaId}", step.StepId, sagaId);
                    }
                }
                catch (Exception ex)
                {
                    compensationResult.IsSuccessful = false;
                    compensationResult.ErrorMessage = ex.Message;
                    compensationResult.CompletedAt = DateTime.UtcNow;

                    // Record metrics and structured logging
                    var compensationDuration = DateTime.UtcNow - compensationResult.StartedAt;
                    _metricsService.RecordCompensation("SaleSaga", step.Action, step.ServiceName, false, compensationDuration);
                    _businessLogger.LogCompensation(sagaId, "SaleSaga", step.Action, step.ServiceName, false, new Dictionary<string, object>
                    {
                        ["duration_seconds"] = compensationDuration.TotalSeconds,
                        ["error_message"] = ex.Message,
                        ["error_type"] = ex.GetType().Name,
                        ["step_id"] = step.StepId
                    });

                    _logger.LogError(ex, "Failed to compensate step {StepId} in saga {SagaId}", step.StepId, sagaId);
                    step.ErrorMessage = ex.Message;

                    // Publish compensation failure event
                    await _eventPublisher.PublishSagaEventAsync(sagaId, "SagaOrchestrator", "CompensationFailure", SagaEventType.Failure, $"Failed to compensate step {step.StepId}: {ex.Message}", new { step.StepId, step.ServiceName, step.Action, Error = ex.Message });
                }

                compensationResults.Add(compensationResult);
            }

            // Determine overall compensation result
            var allSuccessful = compensationResults.All(r => r.IsSuccessful);
            var finalState = allSuccessful ? SagaState.Compensated : SagaState.Failed;
            var finalMessage = allSuccessful
                ? $"All {compensationResults.Count} steps compensated successfully"
                : $"Compensation failed for {compensationResults.Count(r => !r.IsSuccessful)} out of {compensationResults.Count} steps";

            await _stateManager.UpdateSagaStateAsync(sagaId, finalState, "SagaOrchestrator", "CompleteCompensation", SagaEventType.Success, finalMessage);

            // Store compensation results in saga
            saga.CompensationResults = compensationResults;

            _logger.LogInformation("Compensation completed for Saga {SagaId}. Success: {Success}", sagaId, allSuccessful);

            return saga;
        }

        private async Task<SagaStep> ExecuteStepAsync(string sagaId, string serviceName, string action, Func<Task<object>> operation, Func<Task>? compensation = null)
        {
            var step = new SagaStep
            {
                StepId = $"{sagaId}_{serviceName}_{action}_{DateTime.UtcNow.Ticks}",
                ServiceName = serviceName,
                Action = action,
                ExecutedAt = DateTime.UtcNow,
                CompensationAction = compensation
            };

            var stepStartTime = DateTime.UtcNow;
            var sagaType = "SaleSaga"; // This could be determined dynamically

            try
            {
                step.Data = await operation();
                step.IsCompleted = true;

                // Record metrics and structured logging
                var stepDuration = DateTime.UtcNow - stepStartTime;
                _metricsService.RecordSagaStep(sagaType, action, serviceName, true, stepDuration);
                _businessLogger.LogBusinessEvent("step_completed", sagaId, serviceName, new Dictionary<string, object>
                {
                    ["step_name"] = action,
                    ["duration_seconds"] = stepDuration.TotalSeconds,
                    ["step_data"] = step.Data
                });

                _logger.LogInformation("Step {StepId} completed successfully in saga {SagaId}", step.StepId, sagaId);
            }
            catch (Exception ex)
            {
                step.ErrorMessage = ex.Message;
                step.IsCompleted = false;

                // Record metrics and structured logging
                var stepDuration = DateTime.UtcNow - stepStartTime;
                _metricsService.RecordSagaStep(sagaType, action, serviceName, false, stepDuration);
                _businessLogger.LogBusinessEvent("step_failed", sagaId, serviceName, new Dictionary<string, object>
                {
                    ["step_name"] = action,
                    ["duration_seconds"] = stepDuration.TotalSeconds,
                    ["error_message"] = ex.Message,
                    ["error_type"] = ex.GetType().Name
                });

                _logger.LogError(ex, "Step {StepId} failed in saga {SagaId}: {ErrorMessage}", step.StepId, sagaId, ex.Message);
                throw;
            }

            _activeSagas[sagaId].Steps.Add(step);
            return step;
        }
    }
}
