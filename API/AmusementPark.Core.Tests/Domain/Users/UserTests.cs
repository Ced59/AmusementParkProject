using AmusementPark.Core.Domain.Users;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Users;

public sealed class UserTests
{
    [Fact]
    public void Constructor_WhenCreated_ShouldInitializeMutableCollections()
    {
        User user = new User();

        Assert.NotNull(user.Roles);
        Assert.NotNull(user.ExternalLogins);
        Assert.Empty(user.Roles);
        Assert.Empty(user.ExternalLogins);
    }

    [Fact]
    public void HasRole_WhenRoleIsPresent_ShouldReturnTrue()
    {
        User user = new User
        {
            Roles = new List<Role>
            {
                Role.User,
                Role.Admin,
            },
        };

        Boolean result = user.HasRole(Role.Admin);

        Assert.True(result);
    }

    [Fact]
    public void HasRole_WhenRoleIsMissing_ShouldReturnFalse()
    {
        User user = new User
        {
            Roles = new List<Role>
            {
                Role.User,
            },
        };

        Boolean result = user.HasRole(Role.Moderator);

        Assert.False(result);
    }

    [Fact]
    public void HasRole_WhenDuplicateRolesExist_ShouldStillReturnTrue()
    {
        User user = new User
        {
            Roles = new List<Role>
            {
                Role.Admin,
                Role.Admin,
            },
        };

        Boolean result = user.HasRole(Role.Admin);

        Assert.True(result);
    }
}
