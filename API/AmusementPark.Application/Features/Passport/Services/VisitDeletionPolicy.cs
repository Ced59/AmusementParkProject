namespace AmusementPark.Application.Features.Passport.Services;

public static class VisitDeletionPolicy
{
    public const int RetentionDays = 7;

    public const int PurgeBatchSize = 200;

    public static readonly TimeSpan Retention = TimeSpan.FromDays(RetentionDays);

    public static readonly TimeSpan ExportInvalidationClaimDuration =
        TimeSpan.FromMinutes(2);
}
