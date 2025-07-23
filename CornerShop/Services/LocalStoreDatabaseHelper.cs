using Microsoft.Data.Sqlite;

namespace CornerShop.Services
{
    public static class LocalStoreDatabaseHelper
    {
        public static string GetDbPath(string storeId) => $"store_{storeId}.db";

        public static void CreateLocalDatabaseForStore(string storeId)
        {
            string dbPath = GetDbPath(storeId);
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            // Products table
            var createProductsCmd = connection.CreateCommand();
            createProductsCmd.CommandText =
            @"CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT,
                Category TEXT,
                Price REAL,
                StockQuantity INTEGER,
                LastUpdated TEXT
            );";
            createProductsCmd.ExecuteNonQuery();

            // Sales table
            var createSalesCmd = connection.CreateCommand();
            createSalesCmd.CommandText =
            @"CREATE TABLE IF NOT EXISTS Sales (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProductName TEXT,
                Quantity INTEGER,
                Price REAL,
                Date TEXT,
                Status TEXT,
                Synced INTEGER
            );";
            createSalesCmd.ExecuteNonQuery();
        }
    }
}
