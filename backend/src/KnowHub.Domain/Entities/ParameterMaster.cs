namespace KnowHub.Domain.Entities;

public class ParameterMaster : BaseEntity
{
    public string  Name         { get; set; } = string.Empty;
    public string  Code         { get; set; } = string.Empty;
    public string? Description  { get; set; }
    public string  Category     { get; set; } = string.Empty;
    public int     DisplayOrder { get; set; }
    public bool    IsActive     { get; set; } = true;
}
