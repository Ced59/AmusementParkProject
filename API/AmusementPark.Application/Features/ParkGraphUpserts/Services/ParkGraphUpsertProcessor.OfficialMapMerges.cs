using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private async Task<bool> CopyOfficialMapFilesForMergeAsync(
        Park source,
        Park merged,
        JsonElement? sections,
        ParkGraphUpsertResult result,
        CancellationToken cancellationToken)
    {
        if (!ShouldTakeSourceSection(sections, "officialMaps"))
        {
            return true;
        }

        IReadOnlyCollection<ParkOfficialMap> storedMaps = source.OfficialMaps
            .Where(static officialMap => !string.IsNullOrWhiteSpace(officialMap.StorageKey))
            .ToList();
        if (storedMaps.Count == 0)
        {
            return true;
        }

        if (this.parkOfficialMapBinaryStorage is null)
        {
            result.Errors.Add("La fusion des cartes officielles stockées est indisponible car le stockage de fichiers n'est pas configuré.");
            return false;
        }

        foreach (ParkOfficialMap sourceMap in storedMaps)
        {
            ParkOfficialMap? targetMap = merged.OfficialMaps.FirstOrDefault(officialMap =>
                string.Equals(officialMap.Id, sourceMap.Id, StringComparison.OrdinalIgnoreCase));
            if (targetMap is null || string.IsNullOrWhiteSpace(targetMap.StorageKey))
            {
                result.Errors.Add($"La clé cible de la carte officielle '{sourceMap.Id}' n'a pas pu être préparée pour la fusion.");
                return false;
            }

            bool copied = await this.parkOfficialMapBinaryStorage.CopyAsync(
                sourceMap.StorageKey!,
                targetMap.StorageKey,
                cancellationToken);
            if (!copied)
            {
                result.Errors.Add($"Le fichier de la carte officielle '{sourceMap.Id}' est introuvable dans le stockage et la fusion a été annulée.");
                return false;
            }
        }

        return true;
    }
}
