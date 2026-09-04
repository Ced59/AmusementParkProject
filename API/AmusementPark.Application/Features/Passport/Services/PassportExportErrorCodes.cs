namespace AmusementPark.Application.Features.Passport.Services;

public static class PassportExportErrorCodes
{
    public const string InvalidPayload = "passport-export.invalid-payload";
    public const string ExportNotFound = "passport-export.not-found";
    public const string GenerationFailed = "passport-export.generation-failed";
    public const string PersistenceConflict = "passport-export.persistence-conflict";
    public const string TooLarge = "passport-export.too-large";
}
