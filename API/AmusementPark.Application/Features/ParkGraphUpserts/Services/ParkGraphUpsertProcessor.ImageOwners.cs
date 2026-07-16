using System.Text.Json;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private static bool ResolveGraphImageOwner(
        JsonElement patch,
        Park? park,
        Dictionary<string, string> itemKeys,
        Dictionary<string, string> founderKeys,
        Dictionary<string, string> operatorKeys,
        Dictionary<string, string> manufacturerKeys,
        ImageOwnerType requestedOwnerType,
        string? ownerId,
        out ImageOwnerType ownerType,
        out string? resolvedOwnerId)
    {
        ownerType = requestedOwnerType;
        resolvedOwnerId = NormalizeString(ownerId);
        string? ownerKey = ReadString(patch, "ownerKey");

        if (string.Equals(ownerKey, "park", StringComparison.OrdinalIgnoreCase))
        {
            if (park is null)
            {
                return false;
            }

            ownerType = ImageOwnerType.Park;
            resolvedOwnerId = park.Id;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(ownerKey))
        {
            if (TryResolvePrefixedOwnerKey(ownerKey, "operator:", operatorKeys, out string? operatorId))
            {
                ownerType = ImageOwnerType.ParkOperator;
                resolvedOwnerId = operatorId;
                return true;
            }

            if (TryResolvePrefixedOwnerKey(ownerKey, "founder:", founderKeys, out string? founderId))
            {
                ownerType = ImageOwnerType.ParkFounder;
                resolvedOwnerId = founderId;
                return true;
            }

            if (TryResolvePrefixedOwnerKey(ownerKey, "manufacturer:", manufacturerKeys, out string? manufacturerId))
            {
                ownerType = ImageOwnerType.AttractionManufacturer;
                resolvedOwnerId = manufacturerId;
                return true;
            }

            if (TryResolvePrefixedOwnerKey(ownerKey, "standalone-attraction:", itemKeys, out string? standaloneAttractionId)
                || TryResolvePrefixedOwnerKey(ownerKey, "standaloneAttraction:", itemKeys, out standaloneAttractionId))
            {
                ownerType = ImageOwnerType.StandaloneAttraction;
                resolvedOwnerId = standaloneAttractionId;
                return true;
            }
        }

        if (requestedOwnerType == ImageOwnerType.StandaloneAttraction)
        {
            if (!string.IsNullOrWhiteSpace(resolvedOwnerId))
            {
                return true;
            }

            return TryResolveOwnerKey(ownerKey, itemKeys, BuildStandaloneAttractionNameKey(ownerKey), out resolvedOwnerId);
        }

        if (requestedOwnerType == ImageOwnerType.ParkItem)
        {
            return TryResolveOwnerKey(ownerKey, itemKeys, BuildItemNameKey(ownerKey), out resolvedOwnerId);
        }

        if (requestedOwnerType == ImageOwnerType.ParkOperator)
        {
            return TryResolveOwnerKey(ownerKey, operatorKeys, null, out resolvedOwnerId);
        }

        if (requestedOwnerType == ImageOwnerType.ParkFounder)
        {
            return TryResolveOwnerKey(ownerKey, founderKeys, null, out resolvedOwnerId);
        }

        if (requestedOwnerType == ImageOwnerType.AttractionManufacturer)
        {
            return TryResolveOwnerKey(ownerKey, manufacturerKeys, null, out resolvedOwnerId);
        }

        if (!string.IsNullOrWhiteSpace(ownerKey) && TryResolveOwnerKey(ownerKey, itemKeys, BuildItemNameKey(ownerKey), out string? itemId))
        {
            ownerType = ImageOwnerType.ParkItem;
            resolvedOwnerId = itemId;
            return true;
        }

        if (string.IsNullOrWhiteSpace(resolvedOwnerId))
        {
            if (park is null)
            {
                return false;
            }

            ownerType = ImageOwnerType.Park;
            resolvedOwnerId = park.Id;
        }

        return !string.IsNullOrWhiteSpace(resolvedOwnerId);
    }
}
