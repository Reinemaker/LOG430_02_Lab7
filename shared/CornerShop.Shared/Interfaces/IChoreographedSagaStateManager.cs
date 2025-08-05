using CornerShop.Shared.Models;

namespace CornerShop.Shared.Interfaces;

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
