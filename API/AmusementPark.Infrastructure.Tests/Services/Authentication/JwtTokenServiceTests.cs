using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Configuration.Authentication;
using AmusementPark.Infrastructure.Services.Authentication;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Authentication;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void GenerateUserToken_WhenUserHasRoles_ShouldCreateTokenWithLegacyRoleClaims()
    {
        JwtTokenService service = new JwtTokenService(CreateSettings());
        User user = new User
        {
            Id = "user-1",
            Email = "user@example.com",
            FirstName = "John",
            LastName = "Doe",
            AvatarUrl = "https://example.com/avatar.jpg",
            LastLoginUtc = new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc),
            Roles = new List<Role> { Role.User, Role.Admin, Role.Moderator },
        };

        string token = service.GenerateUserToken(user);
        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("issuer", jwt.Issuer);
        Assert.Contains(jwt.Claims, static claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == "user@example.com");
        Assert.Contains(jwt.Claims, static claim => claim.Type == "firstname" && claim.Value == "John");
        Assert.Contains(jwt.Claims, static claim => claim.Type == ClaimTypes.Role && claim.Value == "USER");
        Assert.Contains(jwt.Claims, static claim => claim.Type == ClaimTypes.Role && claim.Value == "ADMIN");
        Assert.Contains(jwt.Claims, static claim => claim.Type == ClaimTypes.Role && claim.Value == "MODERATOR");
    }

    [Fact]
    public void ValidateToken_WhenTokenIsGeneratedByService_ShouldReturnValidResultWithSubject()
    {
        JwtTokenService service = new JwtTokenService(CreateSettings());
        User user = new User { Id = "user-1", Email = "user@example.com", Roles = new List<Role> { Role.User } };
        string token = service.GenerateUserToken(user);

        global::AmusementPark.Application.Ports.TokenValidationResult result = service.ValidateToken(token, validateLifetime: true);

        Assert.True(result.IsValid);
        Assert.Equal("user-1", result.Subject);
        Assert.Contains(result.Claims, static claim => claim.Type == ClaimTypes.Role && claim.Value == "USER");
    }

    [Fact]
    public void ValidateToken_WhenTokenIsInvalid_ShouldReturnInvalidResult()
    {
        JwtTokenService service = new JwtTokenService(CreateSettings());

        global::AmusementPark.Application.Ports.TokenValidationResult result = service.ValidateToken("not-a-token", validateLifetime: true);

        Assert.False(result.IsValid);
        Assert.Null(result.Subject);
    }

    private static JwtSettings CreateSettings()
    {
        return new JwtSettings
        {
            Key = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            Issuer = "issuer",
            Audience = "audience",
            TokenBaseExpirationMinutes = 30,
            TokenRefreshLimitMinutes = 45,
        };
    }
}
