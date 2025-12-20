using MongoDB.Driver;
using Zentec.ProductService.Models.Entities;

namespace Zentec.ProductService.Data
{
    /// <summary>
    /// MongoDB database context
    /// </summary>
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("MongoDB")
                ?? "mongodb://localhost:27017";
            var databaseName = configuration["MongoDB:DatabaseName"] ?? "productservice_db";

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);

            // Ensure indexes are created
            CreateIndexes();
        }

        /// <summary>
        /// Products collection
        /// </summary>
        public IMongoCollection<Product> Products => _database.GetCollection<Product>("products");

        /// <summary>
        /// Create indexes for better query performance
        /// </summary>
        private void CreateIndexes()
        {
            var productCollection = Products;


            // Index on Name (for text search)
            var nameIndexModel = new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Ascending(p => p.Name),
                new CreateIndexOptions { Name = "idx_name" }
            );

            // Index on Category
            var categoryIndexModel = new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Ascending(p => p.Category),
                new CreateIndexOptions { Name = "idx_category" }
            );

            // Index on Brand
            var brandIndexModel = new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Ascending(p => p.Brand),
                new CreateIndexOptions { Name = "idx_brand" }
            );

            // Index on Price
            var priceIndexModel = new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Ascending(p => p.Price),
                new CreateIndexOptions { Name = "idx_price" }
            );

            // Index on IsActive
            var activeIndexModel = new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Ascending(p => p.IsActive),
                new CreateIndexOptions { Name = "idx_isactive" }
            );

            // Compound index on Category + IsActive
            var categoryActiveIndexModel = new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys
                    .Ascending(p => p.Category)
                    .Ascending(p => p.IsActive),
                new CreateIndexOptions { Name = "idx_category_active" }
            );

            // Text index for search
            var textIndexModel = new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Text(p => p.Name).Text(p => p.Description),
                new CreateIndexOptions { Name = "idx_text_search" }
            );

            try
            {
                productCollection.Indexes.CreateMany(new[]
                {
                nameIndexModel,
                categoryIndexModel,
                brandIndexModel,
                priceIndexModel,
                activeIndexModel,
                categoryActiveIndexModel,
                textIndexModel
            });
            }
            catch (Exception)
            {
                // Indexes might already exist
            }
        }
    }
}
