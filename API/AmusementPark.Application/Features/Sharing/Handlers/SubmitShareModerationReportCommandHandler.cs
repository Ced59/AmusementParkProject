using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Services;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class SubmitShareModerationReportCommandHandler
    : ICommandHandler<SubmitShareModerationReportCommand, ApplicationResult>
{
    private readonly ShareModerationService service;

    public SubmitShareModerationReportCommandHandler(ShareModerationService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult> HandleAsync(
        SubmitShareModerationReportCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.SubmitAsync(command, cancellationToken);
    }
}
