using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CornerShop.Models
{
    public class SaleItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("productName")]
        public string ProductName { get; set; } = string.Empty;

        [BsonElement("quantity")]
        public int Quantity { get; set; }

        [BsonElement("price")]
        public decimal Price { get; set; }
    }
}
