using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFit.Handlers;

public sealed class SubmitParkFitSourceReportCommandHandler
    : ICommandHandler<SubmitParkFitSourceReportCommand, ApplicationResult>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkFitSourceReportRepository reportRepository;
    private readonly TimeProvider timeProvider;

    public SubmitParkFitSourceReportCommandHandler(
        IParkRepository parkRepository,
        IParkFitSourceReportRepository reportRepository,
        TimeProvider? timeProvider = null)
    {
        this.parkRepository = parkRepository;
        this.reportRepository = reportRepository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult> HandleAsync(
        SubmitParkFitSourceReportCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        string parkId = command.ParkId?.Trim() ?? string.Empty;
        if (parkId.Length == 0
            || !Enum.IsDefined(command.EvidenceKind)
            || !Enum.IsDefined(command.Reason))
        {
            return ApplicationResult.Failure(ParkFitOperationsApplicationErrors.InvalidReport());
        }

        Park? park = await this.parkRepository.GetByIdAsync(
            parkId,
            includeHidden: false,
            cancellationToken);
        if (park is null || !park.IsPubliclyDiscoverable())
        {
            return ApplicationResult.Failure(ParkFitOperationsApplicationErrors.ParkNotFound());
        }

        string resolvedParkId = park.Id ?? string.Empty;
        string resolvedParkName = park.Name ?? string.Empty;

        ParkFitSourceReport report;
        try
        {
            report = ParkFitSourceReport.Create(
                ParkFitSourceReportId.New(),
                resolvedParkId,
                resolvedParkName,
                command.EvidenceKind,
                command.SourceUrl,
                command.SourceReference,
                command.Reason,
                command.Details,
                this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (ArgumentException)
        {
            return ApplicationResult.Failure(ParkFitOperationsApplicationErrors.InvalidReport());
        }

        ParkFitSourceReportWriteOutcome outcome = await this.reportRepository.CreateAsync(
            report,
            cancellationToken);
        return outcome == ParkFitSourceReportWriteOutcome.Success
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(ParkFitOperationsApplicationErrors.Conflict());
    }
}
