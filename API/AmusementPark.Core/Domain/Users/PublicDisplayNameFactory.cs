namespace AmusementPark.Core.Domain.Users;

/// <summary>
/// Construit des pseudonymes publics sans utiliser de donnée personnelle.
/// </summary>
public static class PublicDisplayNameFactory
{
    public static string Create(IReadOnlyCollection<Role> roles, long publicAccountNumber)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(publicAccountNumber);

        string prefix = ResolvePrefix(roles);
        string format = string.Equals(prefix, "User", StringComparison.Ordinal) ? "D4" : "D2";
        return $"{prefix}{publicAccountNumber.ToString(format, System.Globalization.CultureInfo.InvariantCulture)}";
    }

    private static string ResolvePrefix(IReadOnlyCollection<Role> roles)
    {
        if (roles.Contains(Role.Admin))
        {
            return "Admin";
        }

        return roles.Contains(Role.Moderator) ? "Modo" : "User";
    }
}
