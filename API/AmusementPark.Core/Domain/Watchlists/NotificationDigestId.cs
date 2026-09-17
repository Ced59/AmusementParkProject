using System.Security.Cryptography;
using System.Text;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Watchlists;

public readonly record struct NotificationDigestId
{
    private readonly string? value;

    private NotificationDigestId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized notification digest identifier has no value.");

    public static NotificationDigestId ForGroup(
        string userId,
        NotificationChannel channel,
        NotificationFrequency frequency,
        DateTime periodStartUtc)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        NotificationDigestPeriodResolver.ValidateGroup(channel, frequency, periodStartUtc);
        string canonical = string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{normalizedUserId}\n{(int)channel}\n{(int)frequency}\n{periodStartUtc.Ticks}");
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return new NotificationDigestId(Convert.ToHexString(digest).ToLowerInvariant());
    }

    public static NotificationDigestId Parse(string? value)
    {
        return new NotificationDigestId(IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public override string ToString()
    {
        return this.Value;
    }
}
