using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class DownloadPassportExportQueryHandler
    : IQueryHandler<DownloadPassportExportQuery, ApplicationResult<PassportExportDownload>>
{
    private readonly IPassportExportRepository repository;
    private readonly IPassportClock clock;

    public DownloadPassportExportQueryHandler(
        IPassportExportRepository repository,
        IPassportClock clock)
    {
        this.repository = repository;
        this.clock = clock;
    }

    public async Task<ApplicationResult<PassportExportDownload>> HandleAsync(
        DownloadPassportExportQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId)
            || string.IsNullOrWhiteSpace(query.ExportId)
            || !Guid.TryParseExact(query.ExportId.Trim(), "N", out Guid _))
        {
            return ApplicationResult<PassportExportDownload>.Failure(
                PassportApplicationErrors.ExportNotFound());
        }

        PassportExportDownload? download = await this.repository.GetOwnedDownloadAsync(
            query.ExportId.Trim(),
            query.UserId.Trim(),
            this.clock.UtcNow,
            cancellationToken);
        return download is null
            ? ApplicationResult<PassportExportDownload>.Failure(
                PassportApplicationErrors.ExportNotReady())
            : ApplicationResult<PassportExportDownload>.Success(download);
    }
}
