namespace KnowHub.Domain.Entities;

public class RatingScale : BaseEntity
{
    public string Code         { get; set; } = string.Empty;
    public string Name         { get; set; } = string.Empty;
    public int    NumericValue { get; set; }
    public int    DisplayOrder { get; set; }
    public bool   IsActive     { get; set; } = true;
}
