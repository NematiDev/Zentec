using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Zentec.ProductService.Models.Entities
{
    /// <summary>
    /// Product document for MongoDB
    /// </summary>
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("name")]
        [BsonRequired]
        public string Name { get; set; } = string.Empty;

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("price")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Price { get; set; }

        [BsonElement("stockQuantity")]
        public int StockQuantity { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("brand")]
        public string? Brand { get; set; }

        [BsonElement("category")]
        [BsonRequired]
        public string Category { get; set; } = string.Empty;

        [BsonElement("imageUrl")]
        public string? ImageUrl { get; set; }

        [BsonElement("imageUrls")]
        public List<string>? ImageUrls { get; set; }

        [BsonElement("weight")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Weight { get; set; }

        [BsonElement("tags")]
        public List<string>? Tags { get; set; }

        [BsonElement("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? UpdatedAt { get; set; }
    }
}
