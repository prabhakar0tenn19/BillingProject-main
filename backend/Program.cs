using BillingSystem.Infrastructure;
using BillingSystem.Services;
using BillingSystem.Services.Interfaces;
using QuestPDF.Infrastructure;

// ─── QuestPDF License ─────────────────────────────────────────────────────────
// Community license is free for small businesses and open-source projects.
// Must be set BEFORE any PDF generation call.
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ─── MongoDB Context (Singleton — thread-safe, connection-pooled) ─────────────
builder.Services.AddSingleton<MongoDbContext>();

// ─── HTTP Client Factory (used by PdfService to fetch Cloudinary signature) ───
builder.Services.AddHttpClient();

// ─── Application Services (Scoped — one instance per HTTP request) ────────────
builder.Services.AddScoped<ISettingsService,        SettingsService>();
builder.Services.AddScoped<ICategoryService,         CategoryService>();
builder.Services.AddScoped<IProductService,          ProductService>();
builder.Services.AddScoped<ICustomerService,         CustomerService>();
builder.Services.AddScoped<ICustomerPricingService,  CustomerPricingService>();
builder.Services.AddScoped<IInvoiceService,          InvoiceService>();
builder.Services.AddScoped<IDashboardService,        DashboardService>();
builder.Services.AddScoped<IReportService,           ReportService>();
builder.Services.AddScoped<IPdfService,              PdfService>();

// ─── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Return camelCase JSON (e.g. "partyName" not "PartyName")
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
        // Include null values in response (frontend handles them)
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.Never;
    });

// ─── Swagger / OpenAPI ────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title       = "Billing System API",
        Version     = "v1",
        Description = "Sanitaryware Manufacturing Billing System — GST-compliant invoicing with party-specific pricing"
    });

    // Group endpoints by controller tag for clean Swagger UI
    options.TagActionsBy(api => new[] { api.ActionDescriptor.RouteValues["controller"] ?? "Other" });
    options.DocInclusionPredicate((_, _) => true);

    // Add JWT Bearer Security to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─── JWT Authentication & Authorization ───────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["Key"] ?? "SUPER_SECRET_KEY_AQUA_BILLING_SYSTEM_2026_GOLD_COMPLIANT";
if (secretKey.Length < 32)
{
    secretKey = secretKey.PadRight(32, '0');
}
var key = System.Text.Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"] ?? "AquaIssuer",
        ValidAudience = jwtSection["Audience"] ?? "AquaAudience",
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ─── CORS ─────────────────────────────────────────────────────────────────────
// Allows the React frontend (Vite dev server at 5173) to call the API
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[] { "http://localhost:5173", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ─── Build App ────────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── Seed Database on Startup ─────────────────────────────────────────────────
// Creates default settings + 4 product categories if DB is empty.
// Idempotent — safe to run on every startup.
using (var scope = app.Services.CreateScope())
{
    try
    {
        await DataSeeder.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database seeding failed. App will continue, but check MongoDB connection.");
    }
}

// ─── Middleware Pipeline ──────────────────────────────────────────────────────
// Always enable Swagger for testing, verification, and easy deployment debugging
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Billing System API v1");
    options.RoutePrefix = "swagger";   // Access at /swagger
    options.DocumentTitle = "Billing System API";
});

// CORS must come before UseAuthorization
app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Logger.LogInformation("Billing System API started. Swagger: http://localhost:5000/swagger");

app.Run();
