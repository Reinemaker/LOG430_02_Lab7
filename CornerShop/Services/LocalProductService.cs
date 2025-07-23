using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using CornerShop.Models;

namespace CornerShop.Services
{
    public class LocalProductService
    {
        private readonly string _dbPath;

        public LocalProductService(string storeId)
        {
            _dbPath = LocalStoreDatabaseHelper.GetDbPath(storeId);
        }

        public void AddProduct(Product product)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText =
            @"INSERT INTO Products (Name, Category, Price, StockQuantity, LastUpdated)
              VALUES ($name, $category, $price, $stock, $updated);";
            cmd.Parameters.AddWithValue("$name", product.Name);
            cmd.Parameters.AddWithValue("$category", product.Category);
            cmd.Parameters.AddWithValue("$price", product.Price);
            cmd.Parameters.AddWithValue("$stock", product.StockQuantity);
            cmd.Parameters.AddWithValue("$updated", product.LastUpdated.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        public List<Product> GetAllProducts()
        {
            var products = new List<Product>();
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Products;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                products.Add(new Product
                {
                    Id = reader["Id"].ToString() ?? string.Empty,
                    Name = reader["Name"].ToString() ?? string.Empty,
                    Category = reader["Category"].ToString() ?? string.Empty,
                    Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                    StockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity")),
                    LastUpdated = DateTime.Parse(reader["LastUpdated"].ToString() ?? DateTime.UtcNow.ToString())
                });
            }
            return products;
        }

        // Add UpdateProduct, DeleteProduct, etc. as needed
    }
}
