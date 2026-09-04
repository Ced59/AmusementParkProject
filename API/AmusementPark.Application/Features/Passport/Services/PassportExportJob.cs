namespace AmusementPark.Application.Features.Passport.Services;

public static class PassportExportJob
{
    public const string Kind = "passport.export.generate";

    public const int PayloadVersion = 1;

    public static readonly TimeSpan Retention = TimeSpan.FromHours(1);
}
