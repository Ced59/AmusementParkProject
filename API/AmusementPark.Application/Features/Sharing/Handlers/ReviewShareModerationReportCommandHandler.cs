using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Services;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class ReviewShareModerationReportCommandHandler
    : ICommandHandler<ReviewShareModerationReportCommand, ApplicationResult>
{
    private readonly ShareModerationService service;

    public ReviewShareModerationReportCommandHandler(ShareModerationService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult> HandleAsync(
        ReviewShareModerationReportCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.ReviewAsync(command, cancellationToken);
    }
}
