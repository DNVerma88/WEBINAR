namespace KnowHub.Domain.Entities;

public class RoleParameterMapping : BaseEntity
{
    public string  DesignationCode { get; set; } = string.Empty;
    public Guid    ParameterId     { get; set; }
    public decimal Weightage       { get; set; }
    public int     DisplayOrder    { get; set; }
    public bool    IsMandatory     { get; set; }
    public bool    IsActive        { get; set; } = true;

    // Navigation
    public ParameterMaster Parameter { get; set; } = null!;
}
