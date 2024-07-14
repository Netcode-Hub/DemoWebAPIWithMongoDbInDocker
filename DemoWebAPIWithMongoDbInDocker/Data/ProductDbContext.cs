using MongoDB.Driver;

namespace DemoWebAPIWithMongoDbInDocker.Data
{
    public class ProductDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly IConfiguration Configuration;
        public ProductDbContext(IConfiguration configuration)
        {
            Configuration = configuration;
            string connectionString = Configuration.GetConnectionString("DefaultConnection")!;
            string databaseName = Configuration.GetSection("MongoDB:DatabaseName").Value!;
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }
        public IMongoCollection<Product> Products =>
            _database.GetCollection<Product>(Configuration.GetSection("MongoDB:CollectionName").Value!);
    }
}
