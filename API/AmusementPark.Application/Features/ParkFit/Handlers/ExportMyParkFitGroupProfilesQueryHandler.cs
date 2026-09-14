using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Handlers;

public sealed class ExportMyParkFitGroupProfilesQueryHandler
    : IQueryHandler<ExportMyParkFitGroupProfilesQuery,
        ApplicationResult<ParkFitGroupProfileExportResult>>
{
    private readonly IParkFitGroupProfileRepository repository;
    private readonly TimeProvider timeProvider;

    public ExportMyParkFitGroupProfilesQueryHandler(
        IParkFitGroupProfileRepository repository,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ParkFitGroupProfileExportResult>> HandleAsync(
        ExportMyParkFitGroupProfilesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        string ownerUserId = IdentifierRules.NormalizeRequired(
            query.OwnerUserId,
            nameof(query.OwnerUserId));
        IReadOnlyCollection<ParkFitGroupProfile> profiles =
            await this.repository.ListOwnedAsync(ownerUserId, cancellationToken);
        ParkFitGroupProfileExportItemResult[] items = profiles.Select(static profile =>
            new ParkFitGroupProfileExportItemResult(
                profile.Alias,
                profile.HeightCentimeters,
                profile.AgeYears,
                profile.CanBeAccompanied,
                profile.CompanionAgeYears)).ToArray();
        return ApplicationResult<ParkFitGroupProfileExportResult>.Success(
            new ParkFitGroupProfileExportResult(
                this.timeProvider.GetUtcNow().UtcDateTime,
                items));
    }
}
