using System.Threading.Tasks;

namespace CornerShop.Services
{
    public class StockAlertService
    {
        public Task CheckAndAlertAsync()
        {
            // TODO: Check all stores' SQLite for products below critical threshold
            // TODO: If found, send alert to head office (e.g., insert alert in MongoDB)
            return Task.CompletedTask;
        }
    }
}
