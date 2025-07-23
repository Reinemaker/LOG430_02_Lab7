using System.Threading.Tasks;
using CornerShop.Models;
using System.Collections.Generic;
using System;

namespace CornerShop.Services
{
    public class SyncService
    {
        private readonly IDatabaseService _centralDbService;

        public SyncService(IDatabaseService centralDbService)
        {
            _centralDbService = centralDbService;
        }

        public async Task SyncAllSalesToCentralAsync(string storeId)
        {
            var localSaleService = new LocalSaleService(storeId);
            var unsyncedSales = localSaleService.GetUnsyncedSales(storeId);
            Console.WriteLine($"Found {unsyncedSales.Count} unsynced sales for store {storeId}");
            foreach (var sale in unsyncedSales)
            {
                await _centralDbService.CreateSale(sale);
                localSaleService.MarkSaleAsSynced(sale.Id);
                Console.WriteLine($"Synced sale {sale.Id}");
            }
        }
    }
}
