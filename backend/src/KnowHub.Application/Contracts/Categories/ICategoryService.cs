using KnowHub.Application.Models;

namespace KnowHub.Application.Contracts;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken);
    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
