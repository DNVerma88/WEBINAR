using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KnowHub.Application.Contracts;
using KnowHub.Domain.Entities;
using KnowHub.Domain.Enums;
using KnowHub.Domain.Exceptions;
using KnowHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace KnowHub.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly KnowHubDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthService(KnowHubDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new ValidationException("credentials", "Invalid email or password.");

        if (!user.IsActive)
            throw new ForbiddenException("Your account has been deactivated. Please contact an administrator.");

        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        await PersistRefreshTokenAsync(user.Id, refreshToken, cancellationToken);

        var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var mins) ? mins : 60;

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            User = MapToDto(user)
        };
    }

    public async Task LogoutAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user is null) return;

        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAt = null;
        user.ModifiedOn = DateTime.UtcNow;
        user.RecordVersion++;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.RefreshTokenHash == tokenHash && u.RefreshTokenExpiresAt > DateTime.UtcNow, cancellationToken);

        if (user is null)
            throw new ValidationException("refreshToken", "Invalid or expired refresh token.");

        if (!user.IsActive)
            throw new ForbiddenException("This account has been suspended. Contact an administrator.");

        var newAccessToken = GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshTokenHash = HashToken(newRefreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        user.ModifiedOn = DateTime.UtcNow;
        user.RecordVersion++;
        await _db.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
    }

    private string GenerateAccessToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured.");
        var issuer = _configuration["Jwt:Issuer"] ?? "KnowHub";
        var audience = _configuration["Jwt:Audience"] ?? "KnowHub";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("tenantId", user.TenantId.ToString()),
            new Claim("role", ((int)user.Role).ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private async Task PersistRefreshTokenAsync(Guid userId, string refreshToken, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user is null) return;

        user.RefreshTokenHash = HashToken(refreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        user.ModifiedOn = DateTime.UtcNow;
        user.RecordVersion++;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Slug == request.TenantSlug && t.IsActive, cancellationToken);

        if (tenant is null)
            throw new NotFoundException("Tenant", request.TenantSlug);

        var emailExists = await _db.Users
            .AnyAsync(u => u.TenantId == tenant.Id && u.Email == request.Email, cancellationToken);

        // B23: return a generic validation error to prevent email enumeration
        if (emailExists)
            throw new ValidationException("email", "The provided information is invalid.");

        var systemUserId = tenant.Id; // use tenant id as system actor for self-registration
        var user = new User
        {
            TenantId = tenant.Id,
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Department = request.Department,
            Designation = request.Designation,
            Location = request.Location,
            Role = UserRole.Employee,
            IsActive = true,
            CreatedBy = systemUserId,
            ModifiedBy = systemUserId,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        await PersistRefreshTokenAsync(user.Id, refreshToken, cancellationToken);

        return new RegisterResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            User = MapToDto(user)
        };
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Department = user.Department,
        Designation = user.Designation,
        YearsOfExperience = user.YearsOfExperience,
        Location = user.Location,
        ProfilePhotoUrl = user.ProfilePhotoUrl,
        Role = user.Role,
        IsActive = user.IsActive
    };
}
