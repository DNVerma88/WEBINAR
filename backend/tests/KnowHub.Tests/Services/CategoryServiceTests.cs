using KnowHub.Application.Contracts;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services;
using KnowHub.Tests.TestHelpers;
using Microsoft.Extensions.Caching.Memory;

namespace KnowHub.Tests.Services;

public class CategoryServiceTests
{
    private static CategoryService CreateService(out KnowHub.Infrastructure.Persistence.KnowHubDbContext db, Guid tenantId, Guid userId, UserRole role = UserRole.Admin)
    {
        db = TestDbFactory.CreateInMemory();
        var currentUser = new FakeCurrentUserAccessor { UserId = userId, TenantId = tenantId, Role = role };
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new CategoryService(db, currentUser, cache);
    }

    [Fact]
    public async Task CreateAsync_AdminRole_CreatesCategory()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = CreateService(out var db, tenantId, userId, UserRole.Admin);
        var request = new CreateCategoryRequest { Name = "Engineering", Description = "Tech sessions" };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal("Engineering", result.Name);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_EmployeeRole_ThrowsForbiddenException()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = CreateService(out var db, tenantId, userId, UserRole.Employee);
        var request = new CreateCategoryRequest { Name = "HR" };

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsConflictException()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = CreateService(out var db, tenantId, userId, UserRole.Admin);
        var request = new CreateCategoryRequest { Name = "Engineering" };

        await service.CreateAsync(request, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsOnlyActiveCategories()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = CreateService(out var db, tenantId, userId, UserRole.Admin);

        await service.CreateAsync(new CreateCategoryRequest { Name = "Active" }, CancellationToken.None);
        var inactive = await service.CreateAsync(new CreateCategoryRequest { Name = "ToDelete" }, CancellationToken.None);
        await service.DeleteAsync(inactive.Id, CancellationToken.None);

        var result = await service.GetCategoriesAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }

    [Fact]
    public async Task DeleteAsync_NonExistent_ThrowsNotFoundException()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = CreateService(out var db, tenantId, userId, UserRole.Admin);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.DeleteAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
