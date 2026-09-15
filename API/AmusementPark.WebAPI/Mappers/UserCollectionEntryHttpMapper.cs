using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.WebAPI.Contracts.Watchlists;

namespace AmusementPark.WebAPI.Mappers;

public static class UserCollectionEntryHttpMapper
{
    public static UserCollectionEntryDto ToHttp(this UserCollectionEntryResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new UserCollectionEntryDto
        {
            EntryId = result.EntryId,
            TargetType = result.TargetType.ToString(),
            TargetId = result.TargetId,
            Kind = result.Kind.ToString(),
            TargetStatus = result.TargetStatus.ToString(),
            TargetName = result.TargetName,
            ParentParkId = result.ParentParkId,
            ParentParkName = result.ParentParkName,
            MainImageId = result.MainImageId,
            PrivateNote = result.PrivateNote,
            Priority = result.Priority,
            PreferredStartsOn = result.PreferredStartsOn,
            PreferredEndsOn = result.PreferredEndsOn,
            CreatedAtUtc = result.CreatedAtUtc,
            UpdatedAtUtc = result.UpdatedAtUtc,
            Version = result.Version,
        };
    }

    public static bool TryToApplication(
        string? targetType,
        string? targetId,
        string? kind,
        out UserCollectionTargetInput? input)
    {
        input = null;
        if (!Enum.TryParse(targetType, true, out CollectionTargetType parsedTargetType)
            || !Enum.IsDefined(parsedTargetType)
            || string.IsNullOrWhiteSpace(targetId)
            || !Enum.TryParse(kind, true, out UserCollectionKind parsedKind)
            || !Enum.IsDefined(parsedKind))
        {
            return false;
        }

        input = new UserCollectionTargetInput(
            parsedTargetType,
            targetId.Trim(),
            parsedKind);
        return parsedKind switch
        {
            UserCollectionKind.WantToVisit => parsedTargetType == CollectionTargetType.Park,
            UserCollectionKind.WantToExperience =>
                parsedTargetType == CollectionTargetType.ParkItem,
            _ => true,
        };
    }
}
