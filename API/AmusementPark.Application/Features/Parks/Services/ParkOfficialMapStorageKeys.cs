namespace AmusementPark.Application.Features.Parks.Services;

public static class ParkOfficialMapStorageKeys
{
    public static string Build(string parkId, string officialMapId, string storageVersion, string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parkId);
        ArgumentException.ThrowIfNullOrWhiteSpace(officialMapId);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);
        return $"official-maps/{Uri.EscapeDataString(parkId.Trim())}/{Uri.EscapeDataString(officialMapId.Trim())}.{storageVersion.Trim()}.{extension.TrimStart('.').ToLowerInvariant()}";
    }

    public static bool BelongsTo(string storageKey, string parkId, string officialMapId)
    {
        if (string.IsNullOrWhiteSpace(storageKey)
            || string.IsNullOrWhiteSpace(parkId)
            || string.IsNullOrWhiteSpace(officialMapId))
        {
            return false;
        }

        string expectedPrefix = $"official-maps/{Uri.EscapeDataString(parkId.Trim())}/{Uri.EscapeDataString(officialMapId.Trim())}.";
        return storageKey.StartsWith(expectedPrefix, StringComparison.Ordinal)
            && storageKey.Length > expectedPrefix.Length
            && !storageKey.AsSpan(expectedPrefix.Length).Contains('/');
    }

    public static string? ReassignToPark(
        string storageKey,
        string sourceParkId,
        string targetParkId,
        string officialMapId)
    {
        if (!BelongsTo(storageKey, sourceParkId, officialMapId)
            || string.IsNullOrWhiteSpace(targetParkId))
        {
            return null;
        }

        string sourceDirectory = $"official-maps/{Uri.EscapeDataString(sourceParkId.Trim())}/";
        string targetDirectory = $"official-maps/{Uri.EscapeDataString(targetParkId.Trim())}/";
        return targetDirectory + storageKey[sourceDirectory.Length..];
    }
}
