using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.Search;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private async Task ProcessImagesAsync(JsonElement root, Park park, Dictionary<string, string> itemKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (!root.TryGetProperty("images", out JsonElement images) || images.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement patch in images.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? imageId = ReadString(patch, "imageId") ?? ReadString(patch, "id");
            if (string.IsNullOrWhiteSpace(imageId))
            {
                result.Warnings.Add("Image ignorée : imageId manquant. Le binaire reste géré par l'upload multipart existant.");
                continue;
            }

            Image? image = await this.imageRepository.GetByIdAsync(imageId, cancellationToken);
            if (image is null)
            {
                result.Warnings.Add($"Image '{imageId}' introuvable : rattachement ignoré.");
                continue;
            }

            string? ownerTypeText = ReadString(patch, "ownerType");
            string? ownerId = ReadString(patch, "ownerId");
            ResolveImageOwner(patch, park, itemKeys, ownerTypeText, ownerId, out ImageOwnerType ownerType, out string? resolvedOwnerId);

            ParkGraphUpsertChange change = BuildEntityChange("Image", image.Id, null, image.OriginalFileName ?? image.Id, "Updated", "imageId");
            AddChange(change, "ownerType", image.OwnerType.ToString(), ownerType.ToString());
            AddChange(change, "ownerId", image.OwnerId, resolvedOwnerId);

            if (apply && resolvedOwnerId is not null)
            {
                Image? linked = await this.imageRepository.LinkAsync(image.Id, ownerType, resolvedOwnerId, cancellationToken);
                image = linked ?? image;
            }

            ImageMetadataUpdate metadata = new ImageMetadataUpdate
            {
                Description = HasProperty(patch, "description") ? ReadStringAllowNull(patch, "description") : image.Description,
                AltTexts = HasProperty(patch, "altTexts") ? ToLocalizedTextValues(MergeLocalizedTexts(image.AltTexts, GetArray(patch, "altTexts"), false)) : ToLocalizedTextValues(image.AltTexts),
                Captions = HasProperty(patch, "captions") ? ToLocalizedTextValues(MergeLocalizedTexts(image.Captions, GetArray(patch, "captions"), false)) : ToLocalizedTextValues(image.Captions),
                Credits = HasProperty(patch, "credits") ? ToLocalizedTextValues(MergeLocalizedTexts(image.Credits, GetArray(patch, "credits"), false)) : ToLocalizedTextValues(image.Credits),
                TagIds = HasProperty(patch, "tagIds") ? ReadStringArray(GetArray(patch, "tagIds")) : image.TagIds,
                GeoLocation = image.GeoLocation is null ? null : new GeoPointValue(image.GeoLocation.Latitude, image.GeoLocation.Longitude),
                Category = image.Category,
                IsPublished = HasProperty(patch, "isPublished") ? ReadBool(patch, "isPublished") ?? image.IsPublished : image.IsPublished,
            };

            if (apply)
            {
                await this.imageRepository.UpdateMetadataAsync(image.Id, metadata, cancellationToken);
                if (ReadBool(patch, "setAsCurrent") == true && resolvedOwnerId is not null)
                {
                    await this.imageRepository.SetCurrentAsync(image.Id, ownerType, resolvedOwnerId, cancellationToken);
                }
            }

            result.Changes.Add(change);
        }
    }
}
