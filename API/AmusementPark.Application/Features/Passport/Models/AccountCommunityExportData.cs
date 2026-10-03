namespace AmusementPark.Application.Features.Passport.Models;

public sealed record AccountCommunityExportData(
    IReadOnlyCollection<AccountCommentExportData> Comments,
    IReadOnlyCollection<AccountImageExportData> Images,
    IReadOnlyCollection<AccountHistoricalReportExportData> HistoricalReports,
    IReadOnlyCollection<AccountSocialShareExportData> SocialShares)
{
    public static AccountCommunityExportData Empty { get; } = new AccountCommunityExportData(
        Array.Empty<AccountCommentExportData>(),
        Array.Empty<AccountImageExportData>(),
        Array.Empty<AccountHistoricalReportExportData>(),
        Array.Empty<AccountSocialShareExportData>());
}
