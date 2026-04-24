using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowHub.Infrastructure.Persistence.Configurations;

public class AssessmentGroupConfiguration : IEntityTypeConfiguration<AssessmentGroup>
{
    public void Configure(EntityTypeBuilder<AssessmentGroup> builder)
    {
        builder.ToTable("AssessmentGroups");
        builder.HasKey(g => g.Id).HasName("PK_AssessmentGroups");

        builder.Property(g => g.GroupName).IsRequired().HasMaxLength(200);
        builder.Property(g => g.GroupCode).IsRequired().HasMaxLength(50);
        builder.Property(g => g.Description).HasColumnType("text");
        builder.Property(g => g.AssessmentCategory).HasMaxLength(100).IsRequired(false);

        builder.HasIndex(g => new { g.TenantId, g.GroupCode }).IsUnique()
            .HasDatabaseName("IX_AssessmentGroups_TenantId_GroupCode");
        builder.HasIndex(g => g.TenantId)
            .HasDatabaseName("IX_AssessmentGroups_TenantId");
        builder.HasIndex(g => new { g.TenantId, g.IsActive })
            .HasDatabaseName("IX_AssessmentGroups_TenantId_IsActive");

        builder.HasOne(g => g.PrimaryLead)
            .WithMany()
            .HasForeignKey(g => g.PrimaryLeadUserId)
            .HasConstraintName("FK_AssessmentGroups_Users_PrimaryLeadUserId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(g => g.TenantId)
            .HasConstraintName("FK_AssessmentGroups_Tenants_TenantId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AssessmentGroupMemberConfiguration : IEntityTypeConfiguration<AssessmentGroupMember>
{
    public void Configure(EntityTypeBuilder<AssessmentGroupMember> builder)
    {
        builder.ToTable("AssessmentGroupMembers");
        builder.HasKey(e => e.Id).HasName("PK_AssessmentGroupMembers");

        builder.Property(e => e.EffectiveFrom).HasColumnType("timestamptz");
        builder.Property(e => e.EffectiveTo).HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.TenantId, e.GroupId })
            .HasDatabaseName("IX_AssessmentGroupMembers_TenantId_GroupId");
        builder.HasIndex(e => new { e.TenantId, e.UserId })
            .HasDatabaseName("IX_AssessmentGroupMembers_TenantId_UserId");

        builder.HasOne(e => e.Group)
            .WithMany(g => g.GroupMembers)
            .HasForeignKey(e => e.GroupId)
            .HasConstraintName("FK_AssessmentGroupMembers_AIAssessmentGroups_GroupId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .HasConstraintName("FK_AssessmentGroupMembers_Users_UserId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.WorkRole)
            .WithMany(r => r.GroupMembers)
            .HasForeignKey(e => e.WorkRoleId)
            .HasConstraintName("FK_AssessmentGroupMembers_WorkRoles_WorkRoleId")
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .HasConstraintName("FK_AssessmentGroupMembers_Tenants_TenantId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkRoleConfiguration : IEntityTypeConfiguration<WorkRole>
{
    public void Configure(EntityTypeBuilder<WorkRole> builder)
    {
        builder.ToTable("WorkRoles");
        builder.HasKey(r => r.Id).HasName("PK_WorkRoles");

        builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(150);
        builder.Property(r => r.Category).HasMaxLength(100);

        builder.HasIndex(r => new { r.TenantId, r.Code }).IsUnique()
            .HasDatabaseName("IX_WorkRoles_TenantId_Code");
        builder.HasIndex(r => r.TenantId)
            .HasDatabaseName("IX_WorkRoles_TenantId");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .HasConstraintName("FK_WorkRoles_Tenants_TenantId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AssessmentGroupCoLeadConfiguration : IEntityTypeConfiguration<AssessmentGroupCoLead>
{
    public void Configure(EntityTypeBuilder<AssessmentGroupCoLead> builder)
    {
        builder.ToTable("AssessmentGroupCoLeads");
        builder.HasKey(c => c.Id).HasName("PK_AssessmentGroupCoLeads");

        builder.Property(c => c.EffectiveFrom).HasColumnType("timestamptz");
        builder.Property(c => c.EffectiveTo).HasColumnType("timestamptz");

        builder.HasIndex(c => new { c.TenantId, c.GroupId })
            .HasDatabaseName("IX_AssessmentGroupCoLeads_TenantId_GroupId");
        builder.HasIndex(c => new { c.TenantId, c.UserId })
            .HasDatabaseName("IX_AssessmentGroupCoLeads_TenantId_UserId");

        builder.HasOne(c => c.Group)
            .WithMany(g => g.GroupCoLeads)
            .HasForeignKey(c => c.GroupId)
            .HasConstraintName("FK_AssessmentGroupCoLeads_AIAssessmentGroups_GroupId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .HasConstraintName("FK_AssessmentGroupCoLeads_Users_UserId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .HasConstraintName("FK_AssessmentGroupCoLeads_Tenants_TenantId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AssessmentPeriodConfiguration : IEntityTypeConfiguration<AssessmentPeriod>
{
    public void Configure(EntityTypeBuilder<AssessmentPeriod> builder)
    {
        builder.ToTable("AssessmentPeriods");
        builder.HasKey(p => p.Id).HasName("PK_AssessmentPeriods");

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.StartDate).HasColumnType("date");
        builder.Property(p => p.EndDate).HasColumnType("date");
        builder.Property(p => p.Frequency).HasConversion<int>();
        builder.Property(p => p.Status).HasConversion<int>();

        builder.HasIndex(p => new { p.TenantId, p.Name }).IsUnique()
            .HasDatabaseName("IX_AssessmentPeriods_TenantId_Name");
        builder.HasIndex(p => new { p.TenantId, p.Year, p.Status })
            .HasDatabaseName("IX_AssessmentPeriods_TenantId_Year_Status");
        builder.HasIndex(p => p.TenantId)
            .HasDatabaseName("IX_AssessmentPeriods_TenantId");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .HasConstraintName("FK_AssessmentPeriods_Tenants_TenantId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RatingScaleConfiguration : IEntityTypeConfiguration<RatingScale>
{
    public void Configure(EntityTypeBuilder<RatingScale> builder)
    {
        builder.ToTable("RatingScales");
        builder.HasKey(r => r.Id).HasName("PK_RatingScales");

        builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);

        builder.HasIndex(r => new { r.TenantId, r.Code }).IsUnique()
            .HasDatabaseName("IX_RatingScales_TenantId_Code");
        builder.HasIndex(r => r.TenantId)
            .HasDatabaseName("IX_RatingScales_TenantId");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .HasConstraintName("FK_RatingScales_Tenants_TenantId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RubricDefinitionConfiguration : IEntityTypeConfiguration<RubricDefinition>
{
    public void Configure(EntityTypeBuilder<RubricDefinition> builder)
    {
        builder.ToTable("RubricDefinitions");
        builder.HasKey(r => r.Id).HasName("PK_RubricDefinitions");

        builder.Property(r => r.DesignationCode).IsRequired().HasMaxLength(150);
        builder.Property(r => r.BehaviorDescription).IsRequired().HasColumnType("text");
        builder.Property(r => r.ProcessDescription).IsRequired().HasColumnType("text");
        builder.Property(r => r.EvidenceDescription).IsRequired().HasColumnType("text");
        builder.Property(r => r.EffectiveFrom).HasColumnType("date");
        builder.Property(r => r.EffectiveTo).HasColumnType("date");

        builder.HasIndex(r => new { r.TenantId, r.DesignationCode, r.RatingScaleId })
            .HasDatabaseName("IX_RubricDefinitions_TenantId_DesignationCode_RatingScaleId");
        builder.HasIndex(r => new { r.TenantId, r.IsActive })
            .HasDatabaseName("IX_RubricDefinitions_TenantId_IsActive");
        builder.HasIndex(r => r.TenantId)
            .HasDatabaseName("IX_RubricDefinitions_TenantId");

        builder.HasOne(r => r.RatingScale)
            .WithMany()
            .HasForeignKey(r => r.RatingScaleId)
            .HasConstraintName("FK_RubricDefinitions_RatingScales_RatingScaleId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .HasConstraintName("FK_RubricDefinitions_Tenants_TenantId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ParameterMasterConfiguration : IEntityTypeConfiguration<ParameterMaster>
{
    public void Configure(EntityTypeBuilder<ParameterMaster> builder)
    {
        builder.ToTable("ParameterMasters");
        builder.HasKey(p => p.Id).HasName("PK_ParameterMasters");

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Description).HasColumnType("text");
        builder.Property(p => p.Category).IsRequired().HasMaxLength(100);

        builder.HasIndex(p => new { p.TenantId, p.Code }).IsUnique()
            .HasDatabaseName("IX_ParameterMasters_TenantId_Code");
        builder.HasIndex(p => p.TenantId)
            .HasDatabaseName("IX_ParameterMasters_TenantId");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .HasConstraintName("FK_ParameterMasters_Tenants_TenantId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RoleParameterMappingConfiguration : IEntityTypeConfiguration<RoleParameterMapping>
{
    public void Configure(EntityTypeBuilder<RoleParameterMapping> builder)
    {
        builder.ToTable("RoleParameterMappings");
        builder.HasKey(m => m.Id).HasName("PK_RoleParameterMappings");

        builder.Property(m => m.DesignationCode).IsRequired().HasMaxLength(150);
        builder.Property(m => m.Weightage).HasColumnType("decimal(5,2)");

        builder.HasIndex(m => m.TenantId)
            .HasDatabaseName("IX_RoleParameterMappings_TenantId");

        builder.HasOne(m => m.Parameter)
            .WithMany()
            .HasForeignKey(m => m.ParameterId)
            .HasConstraintName("FK_RoleParameterMappings_ParameterMasters_ParameterId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(m => m.TenantId)
            .HasConstraintName("FK_RoleParameterMappings_Tenants_TenantId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeAssessmentConfiguration : IEntityTypeConfiguration<EmployeeAssessment>
{
    public void Configure(EntityTypeBuilder<EmployeeAssessment> builder)
    {
        builder.ToTable("EmployeeAssessments");
        builder.HasKey(a => a.Id).HasName("PK_EmployeeAssessments");

        builder.Property(a => a.RoleCode).IsRequired().HasMaxLength(150);
        builder.Property(a => a.Designation).HasMaxLength(150);
        builder.Property(a => a.Comment).HasColumnType("text");
        builder.Property(a => a.EvidenceNotes).HasColumnType("text");
        builder.Property(a => a.ParameterSummaryJson).HasColumnType("jsonb");
        builder.Property(a => a.Status).HasConversion<int>();
        builder.Property(a => a.SubmittedOn).HasColumnType("timestamptz");

        builder.HasIndex(a => new { a.TenantId, a.UserId, a.AssessmentPeriodId }).IsUnique()
            .HasDatabaseName("IX_EmployeeAssessments_TenantId_UserId_PeriodId");
        builder.HasIndex(a => new { a.TenantId, a.GroupId })
            .HasDatabaseName("IX_EmployeeAssessments_TenantId_GroupId");
        builder.HasIndex(a => new { a.TenantId, a.AssessmentPeriodId })
            .HasDatabaseName("IX_EmployeeAssessments_TenantId_PeriodId");
        builder.HasIndex(a => new { a.TenantId, a.Status })
            .HasDatabaseName("IX_EmployeeAssessments_TenantId_Status");
        builder.HasIndex(a => a.TenantId)
            .HasDatabaseName("IX_EmployeeAssessments_TenantId");

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .HasConstraintName("FK_EmployeeAssessments_Users_UserId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Group)
            .WithMany(g => g.Assessments)
            .HasForeignKey(a => a.GroupId)
            .HasConstraintName("FK_EmployeeAssessments_AIAssessmentGroups_GroupId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Period)
            .WithMany(p => p.Assessments)
            .HasForeignKey(a => a.AssessmentPeriodId)
            .HasConstraintName("FK_EmployeeAssessments_AssessmentPeriods_AssessmentPeriodId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.RatingScale)
            .WithMany()
            .HasForeignKey(a => a.RatingScaleId)
            .HasConstraintName("FK_EmployeeAssessments_RatingScales_RatingScaleId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Submitter)
            .WithMany()
            .HasForeignKey(a => a.SubmittedBy)
            .HasConstraintName("FK_EmployeeAssessments_Users_SubmittedBy")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .HasConstraintName("FK_EmployeeAssessments_Tenants_TenantId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeAssessmentParameterDetailConfiguration : IEntityTypeConfiguration<EmployeeAssessmentParameterDetail>
{
    public void Configure(EntityTypeBuilder<EmployeeAssessmentParameterDetail> builder)
    {
        builder.ToTable("EmployeeAssessmentParameterDetails");
        builder.HasKey(d => d.Id).HasName("PK_EmployeeAssessmentParameterDetails");

        builder.Property(d => d.Comment).HasColumnType("text");
        builder.Property(d => d.EvidenceNotes).HasColumnType("text");

        builder.HasIndex(d => new { d.TenantId, d.EmployeeAssessmentId, d.ParameterId }).IsUnique()
            .HasDatabaseName("IX_EAPDetails_TenantId_AssessmentId_ParameterId");
        builder.HasIndex(d => new { d.TenantId, d.EmployeeAssessmentId })
            .HasDatabaseName("IX_EAPDetails_TenantId_AssessmentId");

        builder.HasOne(d => d.Assessment)
            .WithMany(a => a.ParameterDetails)
            .HasForeignKey(d => d.EmployeeAssessmentId)
            .HasConstraintName("FK_EAPDetails_EmployeeAssessments_AssessmentId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Parameter)
            .WithMany()
            .HasForeignKey(d => d.ParameterId)
            .HasConstraintName("FK_EAPDetails_ParameterMasters_ParameterId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ParameterRating)
            .WithMany()
            .HasForeignKey(d => d.ParameterRatingScaleId)
            .HasConstraintName("FK_EAPDetails_RatingScales_ParameterRatingScaleId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(d => d.TenantId)
            .HasConstraintName("FK_EAPDetails_Tenants_TenantId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AssessmentAuditLogConfiguration : IEntityTypeConfiguration<AssessmentAuditLog>
{
    public void Configure(EntityTypeBuilder<AssessmentAuditLog> builder)
    {
        builder.ToTable("AssessmentAuditLogs");
        builder.HasKey(a => a.Id).HasName("PK_AssessmentAuditLogs");

        builder.Property(a => a.RelatedEntityType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.OldValueJson).HasColumnType("jsonb");
        builder.Property(a => a.NewValueJson).HasColumnType("jsonb");
        builder.Property(a => a.Remarks).HasColumnType("text");
        builder.Property(a => a.ChangedOn).HasColumnType("timestamptz");
        builder.Property(a => a.ActionType).HasConversion<int>();

        builder.HasIndex(a => new { a.TenantId, a.RelatedEntityId })
            .HasDatabaseName("IX_AssessmentAuditLogs_TenantId_RelatedEntityId");
        builder.HasIndex(a => new { a.TenantId, a.ChangedBy })
            .HasDatabaseName("IX_AssessmentAuditLogs_TenantId_ChangedBy");
        builder.HasIndex(a => a.TenantId)
            .HasDatabaseName("IX_AssessmentAuditLogs_TenantId");

        builder.HasOne(a => a.Assessment)
            .WithMany(e => e.AuditLogs)
            .HasForeignKey(a => a.EmployeeAssessmentId)
            .HasConstraintName("FK_AssessmentAuditLogs_EmployeeAssessments_Id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.ChangedByUser)
            .WithMany()
            .HasForeignKey(a => a.ChangedBy)
            .HasConstraintName("FK_AssessmentAuditLogs_Users_ChangedBy")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .HasConstraintName("FK_AssessmentAuditLogs_Tenants_TenantId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
