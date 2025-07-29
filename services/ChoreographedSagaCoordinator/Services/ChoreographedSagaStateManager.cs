using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace ChoreographedSagaCoordinator.Services;

public interface IChoreographedSagaStateManager
{
    Task<ChoreographedSagaState?> GetSagaStateAsync(string sagaId);
    Task<List<ChoreographedSagaState>> GetAllSagaStatesAsync();
    Task<List<ChoreographedSagaState>> GetSagaStatesByStatusAsync(ChoreographedSagaStatus status);
    Task<List<ChoreographedSagaState>> GetSagaStatesByBusinessProcessAsync(string businessProcess);
    Task SaveSagaStateAsync(ChoreographedSagaState sagaState);
    Task DeleteSagaStateAsync(string sagaId);
    Task<List<ChoreographedSagaState>> GetSagaStatesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<ChoreographedSagaStatistics> GetSagaStatisticsAsync();
}

public class ChoreographedSagaStateManager : IChoreographedSagaStateManager
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ChoreographedSagaStateManager> _logger;

    public ChoreographedSagaStateManager(IConnectionMultiplexer redis, ILogger<ChoreographedSagaStateManager> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<ChoreographedSagaState?> GetSagaStateAsync(string sagaId)
    {
        try
        {
            var db = _redis.GetDatabase();
            var sagaStateJson = await db.StringGetAsync($"choreographed_saga:{sagaId}");
            
            if (sagaStateJson.IsNull)
                return null;

            return JsonSerializer.Deserialize<ChoreographedSagaState>(sagaStateJson!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving saga state for Saga: {SagaId}", sagaId);
            return null;
        }
    }

    public async Task<List<ChoreographedSagaState>> GetAllSagaStatesAsync()
    {
        try
        {
            var db = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: "choreographed_saga:*");
            
            var sagaStates = new List<ChoreographedSagaState>();
            
            foreach (var key in keys)
            {
                var sagaStateJson = await db.StringGetAsync(key);
                if (!sagaStateJson.IsNull)
                {
                    var sagaState = JsonSerializer.Deserialize<ChoreographedSagaState>(sagaStateJson!);
                    if (sagaState != null)
                        sagaStates.Add(sagaState);
                }
            }

            return sagaStates.OrderByDescending(s => s.StartedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all saga states");
            return new List<ChoreographedSagaState>();
        }
    }

    public async Task<List<ChoreographedSagaState>> GetSagaStatesByStatusAsync(ChoreographedSagaStatus status)
    {
        try
        {
            var allSagas = await GetAllSagaStatesAsync();
            return allSagas.Where(s => s.Status == status).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving saga states by status: {Status}", status);
            return new List<ChoreographedSagaState>();
        }
    }

    public async Task<List<ChoreographedSagaState>> GetSagaStatesByBusinessProcessAsync(string businessProcess)
    {
        try
        {
            var allSagas = await GetAllSagaStatesAsync();
            return allSagas.Where(s => s.BusinessProcess == businessProcess).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving saga states by business process: {BusinessProcess}", businessProcess);
            return new List<ChoreographedSagaState>();
        }
    }

    public async Task SaveSagaStateAsync(ChoreographedSagaState sagaState)
    {
        try
        {
            var db = _redis.GetDatabase();
            var sagaStateJson = JsonSerializer.Serialize(sagaState);
            await db.StringSetAsync($"choreographed_saga:{sagaState.SagaId}", sagaStateJson, TimeSpan.FromHours(24));
            
            _logger.LogDebug("Saga state saved: {SagaId}", sagaState.SagaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving saga state for Saga: {SagaId}", sagaState.SagaId);
            throw;
        }
    }

    public async Task DeleteSagaStateAsync(string sagaId)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync($"choreographed_saga:{sagaId}");
            
            _logger.LogInformation("Saga state deleted: {SagaId}", sagaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saga state for Saga: {SagaId}", sagaId);
            throw;
        }
    }

    public async Task<List<ChoreographedSagaState>> GetSagaStatesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var allSagas = await GetAllSagaStatesAsync();
            return allSagas.Where(s => s.StartedAt >= startDate && s.StartedAt <= endDate).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving saga states by date range: {StartDate} to {EndDate}", startDate, endDate);
            return new List<ChoreographedSagaState>();
        }
    }

    public async Task<ChoreographedSagaStatistics> GetSagaStatisticsAsync()
    {
        try
        {
            var allSagas = await GetAllSagaStatesAsync();
            
            var statistics = new ChoreographedSagaStatistics
            {
                TotalSagas = allSagas.Count,
                CompletedSagas = allSagas.Count(s => s.Status == ChoreographedSagaStatus.Completed),
                FailedSagas = allSagas.Count(s => s.Status == ChoreographedSagaStatus.Failed),
                InProgressSagas = allSagas.Count(s => s.Status == ChoreographedSagaStatus.InProgress),
                CompensatedSagas = allSagas.Count(s => s.Steps.Any(step => step.Compensated)),
                AverageDurationSeconds = allSagas
                    .Where(s => s.CompletedAt.HasValue)
                    .Select(s => (s.CompletedAt.Value - s.StartedAt).TotalSeconds)
                    .DefaultIfEmpty(0)
                    .Average(),
                BusinessProcessBreakdown = allSagas
                    .GroupBy(s => s.BusinessProcess)
                    .Select(g => new BusinessProcessStats
                    {
                        BusinessProcess = g.Key,
                        TotalCount = g.Count(),
                        CompletedCount = g.Count(s => s.Status == ChoreographedSagaStatus.Completed),
                        FailedCount = g.Count(s => s.Status == ChoreographedSagaStatus.Failed)
                    })
                    .ToList()
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating saga statistics");
            return new ChoreographedSagaStatistics();
        }
    }
} 