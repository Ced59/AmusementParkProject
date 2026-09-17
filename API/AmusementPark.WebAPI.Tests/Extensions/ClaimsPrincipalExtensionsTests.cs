using System.Security.Claims;
using AmusementPark.WebAPI.Contracts.Users;
using AmusementPark.WebAPI.Extensions;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Extensions;

public sealed class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_WhenNameIdentifierExists_ShouldReturnUserId()
    {
        ClaimsPrincipal user = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, "user-1"));

        string? result = user.GetUserId();

        Assert.Equal("user-1", result);
    }

    [Fact]
    public void GetUserId_WhenNameIdentifierIsMissing_ShouldReturnNull()
    {
        ClaimsPrincipal user = CreatePrincipal();

        string? result = user.GetUserId();

        Assert.Null(result);
    }

    [Fact]
    public void GetUserId_WhenPrincipalIsNull_ShouldThrow()
    {
        ClaimsPrincipal? user = null;

        Assert.Throws<ArgumentNullException>(() => user!.GetUserId());
    }

    [Fact]
    public void GetLastAuthenticationUtc_WhenClaimIsAnIsoTimestamp_ShouldReturnUtcInstant()
    {
        DateTime authenticatedAtUtc = new(2026, 9, 17, 8, 30, 0, DateTimeKind.Utc);
        ClaimsPrincipal user = CreatePrincipal(new Claim("lastlogin", authenticatedAtUtc.ToString("o")));

        DateTime? result = user.GetLastAuthenticationUtc();

        Assert.Equal(authenticatedAtUtc, result);
        Assert.Equal(DateTimeKind.Utc, result!.Value.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-date")]
    public void GetLastAuthenticationUtc_WhenClaimIsMissingOrInvalid_ShouldReturnNull(string? value)
    {
        ClaimsPrincipal user = value is null
            ? CreatePrincipal()
            : CreatePrincipal(new Claim("lastlogin", value));

        DateTime? result = user.GetLastAuthenticationUtc();

        Assert.Null(result);
    }

    [Fact]
    public void IsInRoles_WhenAnyRequestedRoleMatchesIgnoringCase_ShouldReturnTrue()
    {
        ClaimsPrincipal user = CreatePrincipal(
            new Claim(ClaimTypes.Role, "user"),
            new Claim(ClaimTypes.Role, "ADMIN"));

        bool result = user.IsInRoles(UserRoleDto.ADMIN, UserRoleDto.MODERATOR);

        Assert.True(result);
    }

    [Fact]
    public void IsInRoles_WhenNoRequestedRoleMatches_ShouldReturnFalse()
    {
        ClaimsPrincipal user = CreatePrincipal(new Claim(ClaimTypes.Role, "USER"));

        bool result = user.IsInRoles(UserRoleDto.ADMIN, UserRoleDto.MODERATOR);

        Assert.False(result);
    }

    [Fact]
    public void IsInRoles_WhenRoleClaimIsUnknown_ShouldIgnoreUnknownRole()
    {
        ClaimsPrincipal user = CreatePrincipal(new Claim(ClaimTypes.Role, "SUPERADMIN"));

        bool result = user.IsInRoles(UserRoleDto.ADMIN);

        Assert.False(result);
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        ClaimsIdentity identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }
}
