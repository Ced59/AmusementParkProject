using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Application.Features.ParkItems.Services;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class ApplyParkItemsBulkCreateCommandHandler
    : ICommandHandler<ApplyParkItemsBulkCreateCommand, ApplicationResult<ParkItemsBulkCreateApplyResult>>
{
    private readonly ParkItemsBulkCreatePreviewService previewService;
    private readonly IParkItemRepository parkItemRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;
    private readonly IPublicSeoUpdateNotifier publicSeoUpdateNotifier;

    public ApplyParkItemsBulkCreateCommandHandler(
        ParkItemsBulkCreatePreviewService previewService,
        IParkItemRepository parkItemRepository,
        ISearchProjectionWriter searchProjectionWriter,
        IPublicSeoUpdateNotifier publicSeoUpdateNotifier)
    {
        this.previewService = previewService;
        this.parkItemRepository = parkItemRepository;
        this.searchProjectionWriter = searchProjectionWriter;
        this.publicSeoUpdateNotifier = publicSeoUpdateNotifier;
    }

    public async Task<ApplicationResult<ParkItemsBulkCreateApplyResult>> HandleAsync(
        ApplyParkItemsBulkCreateCommand command,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkItemsBulkCreatePreviewResult> previewResult = await this.previewService.PreviewAsync(command.ParkId, command.Rows, cancellationToken);
        if (!previewResult.IsSuccess || previewResult.Value is null)
        {
            return ApplicationResult<ParkItemsBulkCreateApplyResult>.Failure(previewResult.Errors);
        }

        List<string> createdIds = new List<string>();
        List<ParkItem> createdItems = new List<ParkItem>();
        foreach (ParkItemBulkCreatePreviewRow row in previewResult.Value.Rows.Where(static item => item.CanApply))
        {
            ParkItem parkItem = this.previewService.ToParkItem(command.ParkId.Trim(), row);
            ParkItem created = await this.parkItemRepository.CreateAsync(parkItem, cancellationToken);
            createdIds.Add(created.Id);
            createdItems.Add(created);
        }

        if (createdIds.Count > 0)
        {
            await this.searchProjectionWriter.UpsertManyAsync(SearchProjectionResourceTypes.ParkItems, createdIds, cancellationToken);
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, command.ParkId.Trim(), cancellationToken);
            await this.publicSeoUpdateNotifier.NotifyAsync(
                new PublicSeoUpdate
                {
                    CurrentParkItems = PublicSeoParkItemSnapshot.FromParkItems(createdItems),
                },
                cancellationToken);
        }

        ParkItemsBulkCreateApplyResult result = new ParkItemsBulkCreateApplyResult
        {
            Rows = previewResult.Value.Rows,
            CreatedIds = createdIds,
            RequestedCount = previewResult.Value.Rows.Count,
            CreatedCount = createdIds.Count,
            IgnoredCount = previewResult.Value.Rows.Count - createdIds.Count,
        };

        return ApplicationResult<ParkItemsBulkCreateApplyResult>.Success(result);
    }
}
