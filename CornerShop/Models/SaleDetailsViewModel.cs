namespace CornerShop.Models
{
    public class SaleDetailsViewModel
    {
        public Sale Sale { get; set; } = null!;
        public string StoreName { get; set; } = string.Empty;
        public string StoreLocation { get; set; } = string.Empty;
        public List<SaleProductDetails> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
    }

    public class SaleProductDetails
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }
}
