using BillingSystem.Infrastructure;
using BillingSystem.Models;
using MongoDB.Driver;

namespace BillingSystem.Infrastructure;

/// <summary>
/// Seeds the database with required initial data on first run.
/// Called from Program.cs after the app is built.
///
/// Seeds:
///   1. Default company settings (single document)
///   2. Four default product categories (Taps, Showers, Drain Covers, Washbasins)
///
/// All seed operations are idempotent — safe to run on every startup.
/// Checks existence before inserting.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<MongoDbContext>();
        var logger = services.GetRequiredService<ILogger<MongoDbContext>>();

        await SeedSettingsAsync(db, logger);
        await SeedCategoriesAsync(db, logger);
        await SeedUsersAsync(db, logger);
    }

    private static async Task SeedUsersAsync(MongoDbContext db, ILogger logger)
    {
        var count = await db.Users.CountDocumentsAsync(_ => true);
        if (count > 0)
        {
            return;
        }

        var admin = new User
        {
            Username = "admin",
            FullName = "System Administrator",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        admin.PasswordHash = hasher.HashPassword(admin, "Admin@123");

        await db.Users.InsertOneAsync(admin);
        logger.LogInformation("Default administrator user seeded successfully (username: admin, password: Admin@123)");
    }

    // ─── Settings Seed ────────────────────────────────────────────────────────

    private static async Task SeedSettingsAsync(MongoDbContext db, ILogger logger)
    {
        var existing = await db.Settings.Find(_ => true).FirstOrDefaultAsync();
        if (existing is not null)
        {
            logger.LogInformation("Settings already exist — skipping seed");
            return;
        }

        var defaultSettings = new Settings
        {
            CompanyName        = "Your Company Name",
            CompanyAddress     = "Your Company Address, City, State - PIN",
            CompanyPhone       = "9876543210",
            CompanyEmail       = "contact@yourcompany.com",
            Gstin              = "00AAAAA0000A1Z0",   // placeholder — must be updated
            BankName           = "State Bank of India",
            BankAccount        = "00000000000",
            BankIfsc           = "SBIN0000000",
            InvoicePrefix      = "INV",
            NextInvoiceNumber  = 1,
            GstRate            = 5.0m,
            UpdatedAt          = DateTime.UtcNow
        };

        await db.Settings.InsertOneAsync(defaultSettings);
        logger.LogInformation("Default settings seeded. IMPORTANT: Update company details via Settings page.");
    }

    // ─── Categories Seed ──────────────────────────────────────────────────────

    private static async Task SeedCategoriesAsync(MongoDbContext db, ILogger logger)
    {
        var count = await db.Categories.CountDocumentsAsync(_ => true);
        if (count > 0)
        {
            logger.LogInformation("Categories already exist ({Count}) — skipping seed", count);
            return;
        }

        // Four core sanitaryware product categories with correct HSN codes:
        //   8481 → Taps, cocks, valves and similar appliances for pipes, tanks, vats (includes showers)
        //   7325 → Other cast articles of iron or steel (drain covers)
        //   6910 → Ceramic sinks, washbasins, washbasin pedestals, baths, etc.
        var categories = new List<Category>
        {
            new()
            {
                Name        = "Taps",
                Description = "All tap models — kitchen taps, bathroom taps, basin mixers, pillar taps",
                HsnCode     = "8481",
                IsActive    = true,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            },
            new()
            {
                Name        = "Showers",
                Description = "All shower models — rain showers, hand showers, shower sets, overhead showers",
                HsnCode     = "8481",
                IsActive    = true,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            },
            new()
            {
                Name        = "Drain Covers",
                Description = "Floor drain covers, grating, anti-odour floor traps",
                HsnCode     = "7325",
                IsActive    = true,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            },
            new()
            {
                Name        = "Washbasins",
                Description = "Ceramic washbasins, counter top basins, wall-hung basins",
                HsnCode     = "6910",
                IsActive    = true,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            }
        };

        await db.Categories.InsertManyAsync(categories);
        logger.LogInformation("Seeded {Count} default categories: Taps, Showers, Drain Covers, Washbasins",
            categories.Count);
    }
}
