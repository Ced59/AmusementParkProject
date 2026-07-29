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
            UsesAutomaticPublicDisplayName = true,
            Roles = new List<Role> { Role.User, Role.Admin },
        };
        user.AssignPublicAccountNumber(1);

        user.RefreshAutomaticPublicDisplayName();

        Assert.Equal("Admin01", user.PublicDisplayName);
    }

    [Fact]
    public void RefreshAutomaticPublicDisplayName_WhenNicknameWasChosen_ShouldPreserveIt()
    {
        User user = new User
        {
            PublicDisplayName = "CoasterFan",
            UsesAutomaticPublicDisplayName = false,
            Roles = new List<Role> { Role.User, Role.Admin },
        };
        user.AssignPublicAccountNumber(1);

        user.RefreshAutomaticPublicDisplayName();

        Assert.Equal("CoasterFan", user.PublicDisplayName);
    }

    [Fact]
    public void AssignPublicAccountNumber_WhenNumberAlreadyAssigned_ShouldPreserveIt()
    {
        User user = new User();
        user.AssignPublicAccountNumber(1);

        user.AssignPublicAccountNumber(1);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => user.AssignPublicAccountNumber(2));

        Assert.Equal(1, user.PublicAccountNumber);
        Assert.Contains("cannot be changed", exception.Message, StringComparison.Ordinal);
    }
}
