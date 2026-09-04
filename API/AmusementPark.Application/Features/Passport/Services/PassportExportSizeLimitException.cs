namespace AmusementPark.Application.Features.Passport.Services;

public sealed class PassportExportSizeLimitException : InvalidOperationException
{
    public PassportExportSizeLimitException()
        : base("The passport export exceeds the supported artifact size.")
    {
    }
}
