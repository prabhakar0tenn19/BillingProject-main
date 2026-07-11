using BillingSystem.DTOs;

namespace BillingSystem.Services.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryResponse>> GetAllAsync();
    Task<CategoryResponse?> GetByIdAsync(string id);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request);
    Task<CategoryResponse?> UpdateAsync(string id, UpdateCategoryRequest request);

    /// <summary>Soft delete — sets isActive = false. Products in category remain.</summary>
    Task<bool> DeleteAsync(string id);
}
