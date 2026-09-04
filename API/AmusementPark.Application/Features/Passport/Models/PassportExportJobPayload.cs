namespace AmusementPark.Application.Features.Passport.Models;

public sealed record PassportExportJobPayload(
    string ExportId,
    string UserId,
    PassportExportFormat Format);
