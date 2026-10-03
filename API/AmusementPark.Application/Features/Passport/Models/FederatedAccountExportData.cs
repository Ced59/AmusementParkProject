namespace AmusementPark.Application.Features.Passport.Models;

public sealed record FederatedAccountExportData(
    AccountIdentityExportData Identity,
    IReadOnlyCollection<AccountRatingExportData> Ratings,
    IReadOnlyCollection<AccountParkFitProfileExportData> ParkFitProfiles,
    IReadOnlyCollection<AccountTripExportData> Trips,
    AccountCommunityExportData Community)
{
    public static FederatedAccountExportData Empty { get; } = new FederatedAccountExportData(
        new AccountIdentityExportData(
            null,
            null,
            null,
            null,
            false,
            false,
            null,
            null,
            false,
            Array.Empty<string>(),
            Array.Empty<AccountLinkedLoginExportData>(),
            DateTime.UnixEpoch,
            DateTime.UnixEpoch,
            DateTime.UnixEpoch,
            DateTime.UnixEpoch),
        Array.Empty<AccountRatingExportData>(),
        Array.Empty<AccountParkFitProfileExportData>(),
        Array.Empty<AccountTripExportData>(),
        AccountCommunityExportData.Empty);
}
