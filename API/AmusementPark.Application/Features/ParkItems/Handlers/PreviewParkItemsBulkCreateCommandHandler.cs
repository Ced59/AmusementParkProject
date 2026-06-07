using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Application.Features.ParkItems.Services;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class PreviewParkItemsBulkCreateCommandHandler
    : ICommandHandler<PreviewParkItemsBulkCreateCommand, ApplicationResult<ParkItemsBulkCreatePreviewResult>>
{
    private readonly ParkItemsBulkCreatePreviewService previewService;

    public PreviewParkItemsBulkCreateCommandHandler(ParkItemsBulkCreatePreviewService previewService)
    {
        this.previewService = previewService;
    }

    public Task<ApplicationResult<ParkItemsBulkCreatePreviewResult>> HandleAsync(
        PreviewParkItemsBulkCreateCommand command,
        CancellationToken cancellationToken = default)
    {
        return this.previewService.PreviewAsync(command.ParkId, command.Rows, cancellationToken);
    }
}
