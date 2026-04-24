using KnowHub.Domain.Enums;

namespace KnowHub.Application.Contracts;

public class UserDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Department { get; init; }
    public string? Designation { get; init; }
    public int? YearsOfExperience { get; init; }
    public string? Location { get; init; }
    public string? ProfilePhotoUrl { get; init; }
    public UserRole Role { get; init; }
    public bool IsActive { get; init; }
    public int RecordVersion { get; init; }
}

public class GetUsersRequest
{
    public string? SearchTerm { get; set; }
    public string? Department { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class UpdateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Designation { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? Location { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public int RecordVersion { get; set; }
}

public class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Employee;
    public string? Department { get; set; }
    public string? Designation { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? Location { get; set; }
}

public class AdminUpdateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public string? Department { get; set; }
    public string? Designation { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? Location { get; set; }
    public int RecordVersion { get; set; }
}
