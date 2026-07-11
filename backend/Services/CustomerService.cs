using BillingSystem.DTOs;
using BillingSystem.Infrastructure;
using BillingSystem.Models;
using BillingSystem.Services.Interfaces;
using MongoDB.Driver;

namespace BillingSystem.Services;

/// <summary>
/// Customer (party) CRUD service.
/// Customers are businesses/individuals the company sells to.
/// Soft delete — keeps invoice history intact.
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly MongoDbContext _db;

    public CustomerService(MongoDbContext db) => _db = db;

    public async Task<List<CustomerResponse>> GetAllAsync(string? search = null)
    {
        var filterBuilder = Builders<Customer>.Filter;
        var filter = filterBuilder.Eq(c => c.IsActive, true);

        if (!string.IsNullOrEmpty(search))
        {
            var regex = new MongoDB.Bson.BsonRegularExpression(search, "i");
            filter &= filterBuilder.Or(
                filterBuilder.Regex(c => c.PartyName, regex),
                filterBuilder.Regex(c => c.ContactPerson!, regex),
                filterBuilder.Regex(c => c.Phone!, regex),
                filterBuilder.Regex(c => c.Gstin!, regex)
            );
        }

        var customers = await _db.Customers
            .Find(filter)
            .SortBy(c => c.PartyName)
            .ToListAsync();

        return customers.Select(ToDto).ToList();
    }

    /// <summary>Lightweight list for party dropdowns on New Bill page.</summary>
    public async Task<List<CustomerListItem>> GetListAsync()
    {
        var customers = await _db.Customers
            .Find(c => c.IsActive)
            .SortBy(c => c.PartyName)
            .ToListAsync();

        return customers.Select(c => new CustomerListItem(
            c.Id!, c.PartyName, c.Phone, c.Gstin, c.IsActive
        )).ToList();
    }

    public async Task<CustomerResponse?> GetByIdAsync(string id)
    {
        var customer = await _db.Customers.Find(c => c.Id == id).FirstOrDefaultAsync();
        return customer is null ? null : ToDto(customer);
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            PartyName       = request.PartyName.Trim(),
            ContactPerson   = request.ContactPerson?.Trim(),
            Phone           = request.Phone?.Trim(),
            Email           = request.Email?.Trim().ToLowerInvariant(),
            BillingAddress  = request.BillingAddress?.Trim(),
            ShippingAddress = request.ShippingAddress?.Trim(),
            Gstin           = request.Gstin?.Trim().ToUpperInvariant(),
            PanNumber       = request.PanNumber?.Trim().ToUpperInvariant(),
            IsActive        = true,
            CreatedAt       = DateTime.UtcNow,
            UpdatedAt       = DateTime.UtcNow
        };

        await _db.Customers.InsertOneAsync(customer);
        return ToDto(customer);
    }

    public async Task<CustomerResponse?> UpdateAsync(string id, UpdateCustomerRequest request)
    {
        var update = Builders<Customer>.Update
            .Set(c => c.PartyName,       request.PartyName.Trim())
            .Set(c => c.ContactPerson,   request.ContactPerson?.Trim())
            .Set(c => c.Phone,           request.Phone?.Trim())
            .Set(c => c.Email,           request.Email?.Trim().ToLowerInvariant())
            .Set(c => c.BillingAddress,  request.BillingAddress?.Trim())
            .Set(c => c.ShippingAddress, request.ShippingAddress?.Trim())
            .Set(c => c.Gstin,           request.Gstin?.Trim().ToUpperInvariant())
            .Set(c => c.PanNumber,       request.PanNumber?.Trim().ToUpperInvariant())
            .Set(c => c.IsActive,        request.IsActive)
            .Set(c => c.UpdatedAt,       DateTime.UtcNow);

        var result = await _db.Customers.FindOneAndUpdateAsync<Customer, Customer>(
            c => c.Id == id,
            update,
            new FindOneAndUpdateOptions<Customer, Customer> { ReturnDocument = ReturnDocument.After }
        );

        return result is null ? null : ToDto(result);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _db.Customers.UpdateOneAsync(
            c => c.Id == id,
            Builders<Customer>.Update
                .Set(c => c.IsActive,  false)
                .Set(c => c.UpdatedAt, DateTime.UtcNow)
        );

        return result.ModifiedCount > 0;
    }

    // ─── DTO Mapping ──────────────────────────────────────────────────────────

    private static CustomerResponse ToDto(Customer c) => new(
        Id:              c.Id!,
        PartyName:       c.PartyName,
        ContactPerson:   c.ContactPerson,
        Phone:           c.Phone,
        Email:           c.Email,
        BillingAddress:  c.BillingAddress,
        ShippingAddress: c.ShippingAddress,
        Gstin:           c.Gstin,
        PanNumber:       c.PanNumber,
        IsActive:        c.IsActive,
        CreatedAt:       c.CreatedAt,
        UpdatedAt:       c.UpdatedAt
    );
}
