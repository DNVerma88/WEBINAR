using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public class CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? IconName { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public int RecordVersion { get; init; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconName { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconName { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int RecordVersion { get; set; }
}
