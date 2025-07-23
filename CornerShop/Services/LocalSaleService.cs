using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using CornerShop.Models;

namespace CornerShop.Services
{
    public class LocalSaleService
    {
        private readonly string _dbPath;

        public LocalSaleService(string storeId)
        {
            _dbPath = LocalStoreDatabaseHelper.GetDbPath(storeId);
        }

        public void AddSale(Sale sale)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            foreach (var item in sale.Items)
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText =
                @"INSERT INTO Sales (ProductName, Quantity, Price, Date, Status, Synced)
                  VALUES ($productName, $quantity, $price, $date, $status, $synced);";
                cmd.Parameters.AddWithValue("$productName", item.ProductName);
                cmd.Parameters.AddWithValue("$quantity", item.Quantity);
                cmd.Parameters.AddWithValue("$price", item.Price);
                cmd.Parameters.AddWithValue("$date", sale.Date.ToString("o"));
                cmd.Parameters.AddWithValue("$status", sale.Status);
                cmd.Parameters.AddWithValue("$synced", 0); // Not yet synced
                cmd.ExecuteNonQuery();
            }
        }

        public List<Sale> GetAllSales()
        {
            var sales = new List<Sale>();
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Sales;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                // You can expand this to map all fields and items as needed
                sales.Add(new Sale
                {
                    // Map fields as needed
                });
            }
            return sales;
        }

        public List<Sale> GetUnsyncedSales(string storeId)
        {
            var sales = new List<Sale>();
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Sales WHERE Synced = 0;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                sales.Add(new Sale
                {
                    Id = reader["Id"].ToString() ?? string.Empty,
                    StoreId = storeId,
                    Date = DateTime.Parse(reader["Date"].ToString() ?? DateTime.UtcNow.ToString()),
                    Status = reader["Status"].ToString() ?? "Completed",
                    Items = new List<SaleItem>
                    {
                        new SaleItem
                        {
                            ProductName = reader["ProductName"].ToString() ?? string.Empty,
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                            Price = Convert.ToDecimal(reader["Price"])
                        }
                    },
                    TotalAmount = Convert.ToDecimal(reader["Price"]) * Convert.ToInt32(reader["Quantity"])
                });
            }
            return sales;
        }

        public void MarkSaleAsSynced(string saleId)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Sales SET Synced = 1 WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", saleId);
            cmd.ExecuteNonQuery();
        }

        // Add UpdateSale, MarkAsSynced, etc. as needed
    }
}
