using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ContextualBlocks.Commands;
using AmusementPark.Application.Features.ContextualBlocks.Results;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ContextualBlocks.Handlers;

public sealed class ApplyContextualBlockJsonCommandHandler
    : ICommandHandler<ApplyContextualBlockJsonCommand, ApplicationResult<ContextualBlockPreviewResult>>
{
    private readonly ICommandHandler<PreviewContextualBlockJsonCommand, ApplicationResult<ContextualBlockPreviewResult>> previewHandler;
    private readonly IParkRepository parkRepository;
    private readonly ICommandHandler<UpdateParkCommand, ApplicationResult<Park>> updateParkHandler;

    public ApplyContextualBlockJsonCommandHandler(
        ICommandHandler<PreviewContextualBlockJsonCommand, ApplicationResult<ContextualBlockPreviewResult>> previewHandler,
        IParkRepository parkRepository,
        ICommandHandler<UpdateParkCommand, ApplicationResult<Park>> updateParkHandler)
    {
        this.previewHandler = previewHandler;
        this.parkRepository = parkRepository;
        this.updateParkHandler = updateParkHandler;
    }

    public async Task<ApplicationResult<ContextualBlockPreviewResult>> HandleAsync(ApplyContextualBlockJsonCommand command, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ContextualBlockPreviewResult> preview = await this.previewHandler.HandleAsync(
            new PreviewContextualBlockJsonCommand(command.BlockType, command.EntityId, command.Document),
            cancellationToken);

        if (!preview.IsSuccess || preview.Value is null)
        {
            return preview;
        }

        ContextualBlockPreviewResult previewResult = preview.Value;
        if (!previewResult.CanApply || previewResult.Errors.Count > 0)
        {
            return ApplicationResult<ContextualBlockPreviewResult>.Success(previewResult);
        }

        if (previewResult.Changes.Count == 0)
        {
            return ApplicationResult<ContextualBlockPreviewResult>.Success(previewResult);
        }

        string blockType = command.BlockType.Trim();
        string entityId = command.EntityId.Trim();
        Park? park = await this.parkRepository.GetByIdAsync(entityId, true, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ContextualBlockPreviewResult>.Failure(ApplicationErrors.EntityNotFound(nameof(Park), entityId));
        }

        JsonElement block = command.Document.GetProperty("block");
        if (string.Equals(blockType, ContextualBlockContracts.ParkDescriptionBlockType, StringComparison.Ordinal))
        {
            ApplyDescriptionBlock(park, block);
        }
        else if (string.Equals(blockType, ContextualBlockContracts.ParkPracticalBlockType, StringComparison.Ordinal))
        {
            ApplyPracticalBlock(park, block);
        }
        else
        {
            return ApplicationResult<ContextualBlockPreviewResult>.Failure(ContextualBlockApplicationErrors.UnsupportedBlockType(blockType));
        }

        ApplicationResult<Park> updateResult = await this.updateParkHandler.HandleAsync(new UpdateParkCommand(entityId, park), cancellationToken);
        if (!updateResult.IsSuccess)
        {
            return ApplicationResult<ContextualBlockPreviewResult>.Failure(updateResult.Errors);
        }

        previewResult.IsApplied = true;
        return ApplicationResult<ContextualBlockPreviewResult>.Success(previewResult);
    }

    private static void ApplyDescriptionBlock(Park park, JsonElement block)
    {
        JsonElement descriptions = block.GetProperty("descriptions");
        List<LocalizedText> localizedDescriptions = new List<LocalizedText>();
        foreach (JsonElement description in descriptions.EnumerateArray())
        {
            string languageCode = description.GetProperty("languageCode").GetString()?.Trim().ToLowerInvariant() ?? string.Empty;
            JsonElement valueElement = description.GetProperty("value");
            string? value = valueElement.ValueKind == JsonValueKind.Null ? null : valueElement.GetString();
            localizedDescriptions.Add(new LocalizedText(languageCode, value));
        }

        park.Descriptions = localizedDescriptions;
    }

    private static void ApplyPracticalBlock(Park park, JsonElement block)
    {
        ApplyStringField(block, "countryCode", value => park.CountryCode = value);
        ApplyStringField(block, "city", value => park.City = value);
        ApplyStringField(block, "street", value => park.Street = value);
        ApplyStringField(block, "postalCode", value => park.PostalCode = value);
        ApplyStringField(block, "websiteUrl", value => park.WebsiteUrl = value);
        ApplyStringField(block, "founderId", value => park.FounderId = value);
        ApplyStringField(block, "operatorId", value => park.OperatorId = value);
        ApplyPosition(park, block);
    }

    private static void ApplyStringField(JsonElement block, string fieldName, Action<string?> assign)
    {
        if (!block.TryGetProperty(fieldName, out JsonElement valueElement))
        {
            return;
        }

        string? value = valueElement.ValueKind == JsonValueKind.Null ? null : valueElement.GetString();
        assign(value);
    }

    private static void ApplyPosition(Park park, JsonElement block)
    {
        bool hasLatitude = block.TryGetProperty("latitude", out JsonElement latitudeElement);
        bool hasLongitude = block.TryGetProperty("longitude", out JsonElement longitudeElement);
        if (!hasLatitude && !hasLongitude)
        {
            return;
        }

        double? latitude = park.Position?.Latitude;
        double? longitude = park.Position?.Longitude;

        if (hasLatitude)
        {
            latitude = latitudeElement.ValueKind == JsonValueKind.Null ? null : latitudeElement.GetDouble();
        }

        if (hasLongitude)
        {
            longitude = longitudeElement.ValueKind == JsonValueKind.Null ? null : longitudeElement.GetDouble();
        }

        if (latitude.HasValue && longitude.HasValue)
        {
            park.SetPosition(latitude.Value, longitude.Value);
            return;
        }

        if (!latitude.HasValue || !longitude.HasValue)
        {
            park.ClearPosition();
        }
    }
}
