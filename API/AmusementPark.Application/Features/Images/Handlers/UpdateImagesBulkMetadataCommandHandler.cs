using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Ports;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler d'actions de masse sur les métadonnées images.
/// </summary>
public sealed class UpdateImagesBulkMetadataCommandHandler : ICommandHandler<UpdateImagesBulkMetadataCommand, ApplicationResult<BulkAdministrationUpdateResult>>
{
    private readonly IImageRepository imageRepository;

    public UpdateImagesBulkMetadataCommandHandler(IImageRepository imageRepository)
    {
        this.imageRepository = imageRepository;
    }

    public async Task<ApplicationResult<BulkAdministrationUpdateResult>> HandleAsync(UpdateImagesBulkMetadataCommand command, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> imageIds = command.ImageIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (imageIds.Count == 0)
        {
            return ApplicationResult<BulkAdministrationUpdateResult>.Failure(ApplicationErrors.Required(nameof(command.ImageIds)));
        }

        bool hasAction = command.Metadata.IsPublished.HasValue ||
                         command.Metadata.Category.HasValue ||
                         (command.Metadata.AddTagIds?.Count > 0) ||
                         (command.Metadata.RemoveTagIds?.Count > 0);

        if (!hasAction)
        {
            return ApplicationResult<BulkAdministrationUpdateResult>.Failure(ApplicationErrors.Required("bulkImageAction"));
        }

        int updatedCount = await this.imageRepository.UpdateBulkMetadataAsync(imageIds, command.Metadata, cancellationToken);

        return ApplicationResult<BulkAdministrationUpdateResult>.Success(new BulkAdministrationUpdateResult
        {
            RequestedCount = imageIds.Count,
            UpdatedCount = updatedCount,
        });
    }
}
