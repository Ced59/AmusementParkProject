using AmusementPark.Core.Domain.Users;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Users;

public sealed class PublicDisplayNameFactoryTests
{
    [Theory]
    [InlineData(Role.Admin, 1, "Admin01")]
    [InlineData(Role.Moderator, 1, "Modo01")]
    [InlineData(Role.User, 1, "User0001")]
    [InlineData(Role.User, 42, "User0042")]
    public void Create_ShouldUseTheExpectedRolePrefix(
        Role role,
        int ordinal,
        string expected)
    {
        string result = PublicDisplayNameFactory.Create(new[] { role }, ordinal);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Create_ShouldPreferTheHighestStaffRole()
    {
        string result = PublicDisplayNameFactory.Create(
            new[] { Role.User, Role.Moderator, Role.Admin },
            1);

        Assert.Equal("Admin01", result);
    }

    [Theory]
    [InlineData(Role.Admin, 42, "Admin42")]
    [InlineData(Role.Moderator, 42, "Modo42")]
    [InlineData(Role.User, 42, "User0042")]
    public void Create_ShouldKeepTheSameNumericIdentityAcrossRoles(
        Role role,
        long publicAccountNumber,
        string expected)
    {
        string result = PublicDisplayNameFactory.Create(new[] { role }, publicAccountNumber);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void RefreshAutomaticPublicDisplayName_WhenRoleChanges_ShouldKeepTheNumericIdentity()
    {
        User user = new User
        {
            PublicDisplayName = "User0001",
            PublicAccountNumber = 1,
            UsesAutomaticPublicDisplayName = true,
            Roles = new List<Role> { Role.User, Role.Admin },
        };

        user.RefreshAutomaticPublicDisplayName();

        Assert.Equal("Admin01", user.PublicDisplayName);
    }

    [Fact]
    public void RefreshAutomaticPublicDisplayName_WhenNicknameWasChosen_ShouldPreserveIt()
    {
        User user = new User
        {
            PublicDisplayName = "CoasterFan",
            PublicAccountNumber = 1,
            UsesAutomaticPublicDisplayName = false,
            Roles = new List<Role> { Role.User, Role.Admin },
        };

        user.RefreshAutomaticPublicDisplayName();

        Assert.Equal("CoasterFan", user.PublicDisplayName);
    }
}
