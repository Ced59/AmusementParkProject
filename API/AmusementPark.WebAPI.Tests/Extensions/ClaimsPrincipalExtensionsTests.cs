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
