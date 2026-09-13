using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.AttractionAccessConditionTypes;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Contracts;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Ports;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.LocalizedContent.Commands;
using AmusementPark.Application.Features.LocalizedContent.Results;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;
using AmusementPark.Application.Common.Measurements;

namespace AmusementPark.Application.Features.LocalizedContent.Handlers;
/// <summary>
/// Handler d'application d'un JSON localisé sur une entité administrable.
/// </summary>
public sealed class ApplyLocalizedContentJsonCommandHandler : ICommandHandler<ApplyLocalizedContentJsonCommand, ApplicationResult<LocalizedContentApplyResult>>
{
    internal readonly IParkRepository parkRepository;
    internal readonly IParkZoneRepository parkZoneRepository;
    internal readonly IParkItemRepository parkItemRepository;
    internal readonly IParkOperatorRepository parkOperatorRepository;
    internal readonly IParkFounderRepository parkFounderRepository;
    internal readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    internal readonly IImageRepository imageRepository;
    internal readonly IImageTagRepository imageTagRepository;
    internal readonly IAttractionAccessConditionTypeDefinitionRepository accessConditionTypeDefinitionRepository;
    internal readonly ISearchProjectionWriter searchProjectionWriter;
    internal readonly IMeasurementConversionService measurementConversionService;
    public ApplyLocalizedContentJsonCommandHandler(IParkRepository parkRepository, IParkZoneRepository parkZoneRepository, IParkItemRepository parkItemRepository, IParkOperatorRepository parkOperatorRepository, IParkFounderRepository parkFounderRepository, IAttractionManufacturerRepository attractionManufacturerRepository, IImageRepository imageRepository, IImageTagRepository imageTagRepository, IAttractionAccessConditionTypeDefinitionRepository accessConditionTypeDefinitionRepository, ISearchProjectionWriter searchProjectionWriter, IMeasurementConversionService measurementConversionService)
    {
        this.parkRepository = parkRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkOperatorRepository = parkOperatorRepository;
        this.parkFounderRepository = parkFounderRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.imageRepository = imageRepository;
        this.imageTagRepository = imageTagRepository;
        this.accessConditionTypeDefinitionRepository = accessConditionTypeDefinitionRepository;
        this.searchProjectionWriter = searchProjectionWriter;
        this.measurementConversionService = measurementConversionService;
    }

    public async Task<ApplicationResult<LocalizedContentApplyResult>> HandleAsync(ApplyLocalizedContentJsonCommand command, CancellationToken cancellationToken = default)
    {
        if (!LocalizedContentEntityTypeParser.TryParse(command.EntityType, out LocalizedContentEntityType entityType))
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(LocalizedContentApplicationErrors.InvalidEntityType(command.EntityType));
        }

        if (string.IsNullOrWhiteSpace(command.EntityId))
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.Required(nameof(command.EntityId)));
        }

        if (!ApplyLocalizedContentJsonCommandHandlerParsingExtensions.TryParsePatch(command.Json, out LocalizedContentPatch? patch) || patch is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(LocalizedContentApplicationErrors.InvalidJson());
        }

        ApplicationResult<LocalizedContentApplyResult> result = entityType switch
        {
            LocalizedContentEntityType.Park => await this.ApplyToParkAsync(command.EntityId.Trim(), patch, cancellationToken),
            LocalizedContentEntityType.ParkZone => await this.ApplyToParkZoneAsync(command.EntityId.Trim(), patch, cancellationToken),
            LocalizedContentEntityType.ParkItem => await this.ApplyToParkItemAsync(command.EntityId.Trim(), patch, cancellationToken),
            LocalizedContentEntityType.ParkOperator => await this.ApplyToParkOperatorAsync(command.EntityId.Trim(), patch, cancellationToken),
            LocalizedContentEntityType.ParkFounder => await this.ApplyToParkFounderAsync(command.EntityId.Trim(), patch, cancellationToken),
            LocalizedContentEntityType.AttractionManufacturer => await this.ApplyToAttractionManufacturerAsync(command.EntityId.Trim(), patch, cancellationToken),
            LocalizedContentEntityType.Image => await this.ApplyToImageAsync(command.EntityId.Trim(), patch, cancellationToken),
            LocalizedContentEntityType.ImageTag => await this.ApplyToImageTagAsync(command.EntityId.Trim(), patch, cancellationToken),
            LocalizedContentEntityType.AccessConditionType => await this.ApplyToAccessConditionTypeAsync(command.EntityId.Trim(), patch, cancellationToken),
            _ => ApplicationResult<LocalizedContentApplyResult>.Failure(LocalizedContentApplicationErrors.InvalidEntityType(command.EntityType)),
        };
        return result;
    }
}
