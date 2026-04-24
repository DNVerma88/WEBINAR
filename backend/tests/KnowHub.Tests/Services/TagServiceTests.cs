using KnowHub.Application.Contracts;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Services;
using KnowHub.Tests.TestHelpers;

namespace KnowHub.Tests.Services;

public class TagServiceTests
{
    private static TagService CreateService(KnowHub.Infrastructure.Persistence.KnowHubDbContext db, Guid tenantId, Guid userId, UserRole role = UserRole.KnowledgeTeam)
    {
        var currentUser = new FakeCurrentUserAccessor { UserId = userId, TenantId = tenantId, Role = role };
        return new TagService(db, currentUser);
    }

    [Fact]
    public async Task CreateAsync_KnowledgeTeamRole_CreatesTagWithSlug()
    {
        var db = TestDbFactory.CreateInMemory();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = CreateService(db, tenantId, userId, UserRole.KnowledgeTeam);

        var result = await service.CreateAsync(new CreateTagRequest { Name = "React Hooks" }, CancellationToken.None);

        Assert.Equal("React Hooks", result.Name);
        Assert.Equal("react-hooks", result.Slug);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_EmployeeRole_ThrowsForbiddenException()
    {
        var db = TestDbFactory.CreateInMemory();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = CreateService(db, tenantId, userId, UserRole.Employee);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CreateAsync(new CreateTagRequest { Name = "React" }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_DuplicateSlug_ThrowsConflictException()
    {
        var db = TestDbFactory.CreateInMemory();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = CreateService(db, tenantId, userId);

        await service.CreateAsync(new CreateTagRequest { Name = "React" }, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateTagRequest { Name = "react" }, CancellationToken.None));
    }

    [Fact]
    public async Task GetTagsAsync_SearchTerm_FiltersResults()
    {
        var db = TestDbFactory.CreateInMemory();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = CreateService(db, tenantId, userId);

        await service.CreateAsync(new CreateTagRequest { Name = "React" }, CancellationToken.None);
        await service.CreateAsync(new CreateTagRequest { Name = "Angular" }, CancellationToken.None);
        await service.CreateAsync(new CreateTagRequest { Name = "ReactNative" }, CancellationToken.None);

        var result = await service.GetTagsAsync(new GetTagsRequest { SearchTerm = "React" }, CancellationToken.None);

        Assert.Equal(2, result.Data.Count);
        Assert.All(result.Data, t => Assert.Contains("React", t.Name));
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesTag()
    {
        var db = TestDbFactory.CreateInMemory();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = CreateService(db, tenantId, userId, UserRole.Admin);

        var tag = await service.CreateAsync(new CreateTagRequest { Name = "Vue" }, CancellationToken.None);
        await service.DeleteAsync(tag.Id, CancellationToken.None);

        var result = await service.GetTagsAsync(new GetTagsRequest { IsActive = true }, CancellationToken.None);

        Assert.Empty(result.Data);
    }
}
