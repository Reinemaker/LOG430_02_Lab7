using System;

namespace CornerShop.Models
{
    public class StockAlert
    {
        public string ProductId { get; set; } = string.Empty;
        public string StoreId { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public DateTime AlertTime { get; set; }
        public bool Resolved { get; set; }
    }
}
