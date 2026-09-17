using System.Text;
using AmusementPark.Infrastructure.Configuration.Trips;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Configuration.Trips;

public sealed class TripFingerprintKeyRingSettingsTests
{
    [Fact]
    public void GetValidatedKeys_ShouldReturnCurrentAndPreviousVersions()
    {
        TripFingerprintKeyRingSettings settings = new()
        {
            CurrentVersion = "v2",
            CurrentKey = ToBase64("current-trip-fingerprint-key-material"),
            PreviousKeys = $"v1={ToBase64("previous-trip-fingerprint-key-material")}",
        };

        IReadOnlyDictionary<string, string> keys = settings.GetValidatedKeys();

        Assert.Equal(2, keys.Count);
        Assert.Equal(settings.CurrentKey, keys["v2"]);
        Assert.Contains("v1", keys.Keys);
    }

    [Fact]
    public void GetValidatedKeys_ShouldRejectDuplicateCurrentVersion()
    {
        string key = ToBase64("current-trip-fingerprint-key-material");
        TripFingerprintKeyRingSettings settings = new()
        {
            CurrentVersion = "v1",
            CurrentKey = key,
            PreviousKeys = $"v1={key}",
        };

        Assert.Throws<InvalidOperationException>(() => settings.GetValidatedKeys());
    }

    private static string ToBase64(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }
}
