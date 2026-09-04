using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport.Queries;

public sealed record DownloadPassportExportQuery(
    string UserId,
    string ExportId) : IQuery<ApplicationResult<PassportExportDownload>>;
