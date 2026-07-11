using BillingSystem.DTOs;

namespace BillingSystem.Services.Interfaces;

public interface ICustomerService
{
    Task<List<CustomerResponse>> GetAllAsync(string? search = null);
    Task<List<CustomerListItem>> GetListAsync();   // Lightweight for dropdowns
    Task<CustomerResponse?> GetByIdAsync(string id);
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request);
    Task<CustomerResponse?> UpdateAsync(string id, UpdateCustomerRequest request);
    Task<bool> DeleteAsync(string id);
}
