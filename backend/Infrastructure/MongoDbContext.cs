using MongoDB.Driver;
using BillingSystem.Models;

namespace BillingSystem.Infrastructure;

/// <summary>
/// Central MongoDB context — provides strongly-typed collection access to all services.
///
/// Registered as SINGLETON in DI. MongoClient is thread-safe and connection-pooled
/// internally by the driver. Do NOT create new MongoClient per request.
///
/// Collections:
///   - settings       → IMongoCollection&lt;Settings&gt;
///   - categories     → IMongoCollection&lt;Category&gt;
///   - products       → IMongoCollection&lt;Product&gt;
///   - customers      → IMongoCollection&lt;Customer&gt;
///   - customer_pricing → IMongoCollection&lt;CustomerPricing&gt;
///   - invoices       → IMongoCollection&lt;Invoice&gt;
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbContext> _logger;

    public MongoDbContext(IConfiguration configuration, ILogger<MongoDbContext> logger)
    {
        _logger = logger;

        var connectionString = configuration.GetConnectionString("MongoDB")
            ?? throw new InvalidOperationException(
                "MongoDB connection string 'MongoDB' is missing from appsettings.json");

        var databaseName = configuration["MongoDB:DatabaseName"] ?? "billing_db";

        var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
        clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);

        var client = new MongoClient(clientSettings);
        _database = client.GetDatabase(databaseName);

        _logger.LogInformation("MongoDB connected to database: {Database}", databaseName);

        // Create indexes synchronously on startup (safe — idempotent)
        CreateIndexes();
    }

    // ─── Collection Accessors ─────────────────────────────────────────────────

    public IMongoCollection<Settings> Settings
        => _database.GetCollection<Settings>("settings");

    public IMongoCollection<Category> Categories
        => _database.GetCollection<Category>("categories");

    public IMongoCollection<Product> Products
        => _database.GetCollection<Product>("products");

    public IMongoCollection<Customer> Customers
        => _database.GetCollection<Customer>("customers");

    public IMongoCollection<CustomerPricing> CustomerPricing
        => _database.GetCollection<CustomerPricing>("customer_pricing");

    public IMongoCollection<Invoice> Invoices
        => _database.GetCollection<Invoice>("invoices");

    // ─── Index Creation ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates all required MongoDB indexes.
    /// Called once on startup. All operations are idempotent — safe to run repeatedly.
    /// </summary>
    private void CreateIndexes()
    {
        try
        {
            // customer_pricing: unique compound index on (customerId, productId)
            // Enforces: only ONE price override per product per customer
            var pricingIndexKeys = Builders<CustomerPricing>.IndexKeys
                .Ascending(x => x.CustomerId)
                .Ascending(x => x.ProductId);

            CustomerPricing.Indexes.CreateOne(
                new CreateIndexModel<CustomerPricing>(
                    pricingIndexKeys,
                    new CreateIndexOptions
                    {
                        Unique = true,
                        Name = "customer_product_unique",
                        Background = true
                    }));

            // invoices: compound index on (customerId, createdAt DESC)
            // Optimizes: "all invoices for customer X" sorted by newest
            var invoiceByCustomerKeys = Builders<Invoice>.IndexKeys
                .Ascending(x => x.CustomerId)
                .Descending(x => x.CreatedAt);

            Invoices.Indexes.CreateOne(
                new CreateIndexModel<Invoice>(
                    invoiceByCustomerKeys,
                    new CreateIndexOptions { Name = "invoice_customer_date", Background = true }));

            // invoices: index on status for pending payments dashboard
            var invoiceStatusKeys = Builders<Invoice>.IndexKeys.Ascending(x => x.Status);
            Invoices.Indexes.CreateOne(
                new CreateIndexModel<Invoice>(
                    invoiceStatusKeys,
                    new CreateIndexOptions { Name = "invoice_status", Background = true }));

            // invoices: index on invoiceDate for reporting
            var invoiceDateKeys = Builders<Invoice>.IndexKeys.Descending(x => x.InvoiceDate);
            Invoices.Indexes.CreateOne(
                new CreateIndexModel<Invoice>(
                    invoiceDateKeys,
                    new CreateIndexOptions { Name = "invoice_date", Background = true }));

            // products: index on categoryId for filtering
            var productCategoryKeys = Builders<Product>.IndexKeys.Ascending(x => x.CategoryId);
            Products.Indexes.CreateOne(
                new CreateIndexModel<Product>(
                    productCategoryKeys,
                    new CreateIndexOptions { Name = "product_category", Background = true }));

            // customer_pricing: index on customerId for loading party's full catalog
            var pricingCustomerKeys = Builders<CustomerPricing>.IndexKeys.Ascending(x => x.CustomerId);
            CustomerPricing.Indexes.CreateOne(
                new CreateIndexModel<CustomerPricing>(
                    pricingCustomerKeys,
                    new CreateIndexOptions { Name = "pricing_customer", Background = true }));

            _logger.LogInformation("MongoDB indexes created/verified successfully");
        }
        catch (Exception ex)
        {
            // Log but don't crash — indexes might already exist with same definition
            _logger.LogWarning("Index creation warning (non-fatal): {Message}", ex.Message);
        }
    }
}
