using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class GetPassportExportQueryHandler
    : IQueryHandler<GetPassportExportQuery, ApplicationResult<PassportExport>>
{
    private readonly IPassportExportRepository repository;
    private readonly IPassportClock clock;

    public GetPassportExportQueryHandler(
        IPassportExportRepository repository,
        IPassportClock clock)
    {
        this.repository = repository;
        this.clock = clock;
    }

    public async Task<ApplicationResult<PassportExport>> HandleAsync(
        GetPassportExportQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!IsValid(query.UserId, query.ExportId))
        {
            return ApplicationResult<PassportExport>.Failure(
                PassportApplicationErrors.ExportNotFound());
        }

        PassportExport? passportExport = await this.repository.GetOwnedAsync(
            query.ExportId.Trim(),
            query.UserId.Trim(),
            cancellationToken);
        if (passportExport is null)
        {
            return ApplicationResult<PassportExport>.Failure(
                PassportApplicationErrors.ExportNotFound());
        }

        PassportExport result = passportExport.ExpiresAtUtc <= this.clock.UtcNow
            ? passportExport with { Status = PassportExportStatus.Expired }
            : passportExport;
        return ApplicationResult<PassportExport>.Success(result);
    }

    private static bool IsValid(string? userId, string? exportId)
    {
        return !string.IsNullOrWhiteSpace(userId)
            && !string.IsNullOrWhiteSpace(exportId)
            && Guid.TryParseExact(exportId.Trim(), "N", out Guid _);
    }
}
