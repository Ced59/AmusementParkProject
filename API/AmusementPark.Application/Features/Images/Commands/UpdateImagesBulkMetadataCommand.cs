using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;

namespace AmusementPark.Application.Features.Images.Commands;

/// <summary>
/// Applique un patch de métadonnées à plusieurs images.
/// </summary>
public sealed record UpdateImagesBulkMetadataCommand(
    IReadOnlyCollection<string> ImageIds,
    ImageBulkMetadataUpdate Metadata) : ICommand<ApplicationResult<BulkAdministrationUpdateResult>>;
