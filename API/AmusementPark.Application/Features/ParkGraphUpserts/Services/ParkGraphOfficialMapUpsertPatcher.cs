using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.Parks.Services;
using AmusementPark.Core.Domain.Parks;
using static AmusementPark.Application.Features.ParkGraphUpserts.Services.ParkGraphUpsertProcessor;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

internal static class ParkGraphOfficialMapUpsertPatcher
{
    public static void Patch(Park park, JsonElement? parkPatch, ParkGraphUpsertResult result)
    {
        JsonElement? patches = GetArray(parkPatch, "officialMaps");
        if (patches is null)
        {
            if (HasProperty(parkPatch, "officialMaps"))
            {
                result.Errors.Add("park.officialMaps doit être un tableau JSON.");
            }

            return;
        }

        int errorCountBeforePatch = result.Errors.Count;
        List<ParkOfficialMap> officialMaps = park.OfficialMaps
            .Select(static officialMap => CloneOfficialMap(officialMap))
            .ToList();
        HashSet<string> processedIdentities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (JsonElement patch in patches.Value.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                result.Errors.Add("Chaque entrée park.officialMaps doit être un objet JSON.");
                continue;
            }

            string? id = ReadString(patch, "id");
            string? key = ReadString(patch, "key");
            int? requestedYear = ReadInt(patch, "year");
            string? requestedLanguageValue = ReadString(patch, "languageCode");
            string? requestedLanguageCode = NormalizeLanguageCode(requestedLanguageValue);
            ParkOfficialMapFormat? requestedFormat = ReadEnumNullable<ParkOfficialMapFormat>(patch, "format");
            ParkOfficialMap? existing = FindOfficialMap(officialMaps, id, key, requestedYear, requestedLanguageCode, requestedFormat);
            bool isNew = existing is null;
            ParkOfficialMap candidate = existing is null
                ? new ParkOfficialMap
                {
                    Id = id ?? key ?? Guid.NewGuid().ToString("N"),
                    LanguageCode = requestedLanguageCode,
                    IsVisible = false,
                }
                : CloneOfficialMap(existing);
            string matchMode = ResolveOfficialMapMatchMode(existing, id, key);
            ParkGraphUpsertChange change = BuildEntityChange(
                "ParkOfficialMap",
                candidate.Id,
                key,
                BuildOfficialMapDisplayName(candidate, requestedYear),
                isNew ? "Created" : "Unchanged",
                isNew ? "year+languageCode+format" : matchMode);

            PatchInt(patch, "year", candidate.Year, value => candidate.Year = value, change);
            PatchEnum(patch, "format", candidate.Format, value => candidate.Format = value, change);
            PatchString(patch, "documentUrl", candidate.DocumentUrl, value => candidate.DocumentUrl = value, change);
            PatchString(patch, "storageKey", candidate.StorageKey, value => candidate.StorageKey = value, change);
            PatchString(patch, "originalFileName", candidate.OriginalFileName, value => candidate.OriginalFileName = value, change);
            PatchString(patch, "contentType", candidate.ContentType, value => candidate.ContentType = value, change);
            PatchIntNullable(
                patch,
                "sizeInBytes",
                candidate.SizeInBytes is >= 0 and <= int.MaxValue ? (int?)candidate.SizeInBytes.Value : null,
                value => candidate.SizeInBytes = value,
                change);
            PatchString(patch, "previewImageUrl", candidate.PreviewImageUrl, value => candidate.PreviewImageUrl = value, change);
            PatchString(patch, "sourcePageUrl", candidate.SourcePageUrl, value => candidate.SourcePageUrl = value, change);
            PatchString(patch, "languageCode", candidate.LanguageCode, value => candidate.LanguageCode = NormalizeLanguageCode(value), change);
            PatchBool(patch, "isVisible", candidate.IsVisible, value => candidate.IsVisible = value, change);
            PatchDateNullable(patch, "lastVerifiedAtUtc", candidate.LastVerifiedAtUtc, value => candidate.LastVerifiedAtUtc = value, change, "lastVerifiedAtUtc");

            if (HasProperty(patch, "titles"))
            {
                candidate.Titles = PatchLocalizedTexts(candidate.Titles, GetArray(patch, "titles"), false, change, "titles");
            }

            if (HasProperty(patch, "alternativeTexts"))
            {
                candidate.AlternativeTexts = PatchLocalizedTexts(candidate.AlternativeTexts, GetArray(patch, "alternativeTexts"), false, change, "alternativeTexts");
            }

            List<string> validationErrors = ValidateOfficialMap(park.Id, candidate);
            if (isNew && !requestedFormat.HasValue)
            {
                validationErrors.Add($"format est obligatoire pour la nouvelle carte officielle '{candidate.Id}'.");
            }
            if (!string.IsNullOrWhiteSpace(requestedLanguageValue) && requestedLanguageCode is null)
            {
                validationErrors.Add($"languageCode dépasse 16 caractères pour la carte officielle '{candidate.Id}'.");
            }

            string identity = BuildOfficialMapIdentity(candidate);
            if (!processedIdentities.Add(identity))
            {
                validationErrors.Add($"La carte officielle '{identity}' est définie plusieurs fois dans le même lot.");
            }

            ParkOfficialMap? duplicate = officialMaps.FirstOrDefault(officialMap => !ReferenceEquals(officialMap, existing)
                && !string.Equals(officialMap.Id, existing?.Id, StringComparison.OrdinalIgnoreCase)
                && string.Equals(BuildOfficialMapIdentity(officialMap), identity, StringComparison.OrdinalIgnoreCase));
            if (duplicate is not null)
            {
                validationErrors.Add($"Une carte officielle existe déjà pour {candidate.Year}, la langue '{candidate.LanguageCode ?? "non précisée"}' et le format '{candidate.Format}'.");
            }

            if (validationErrors.Count > 0)
            {
                foreach (string validationError in validationErrors)
                {
                    result.Errors.Add(validationError);
                }

                continue;
            }

            change.DisplayName = BuildOfficialMapDisplayName(candidate, null);
            change.ChangeType = isNew ? "Created" : change.Fields.Count > 0 ? "Updated" : "Unchanged";
            result.Changes.Add(change);

            if (isNew)
            {
                officialMaps.Add(candidate);
                continue;
            }

            int existingIndex = officialMaps.FindIndex(officialMap => string.Equals(officialMap.Id, existing!.Id, StringComparison.OrdinalIgnoreCase));
            officialMaps[existingIndex] = candidate;
        }

        if (result.Errors.Count > errorCountBeforePatch)
        {
            return;
        }

        park.OfficialMaps = officialMaps
            .OrderByDescending(static officialMap => officialMap.Year)
            .ThenBy(static officialMap => officialMap.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static officialMap => officialMap.Format)
            .ThenBy(static officialMap => officialMap.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static ParkOfficialMap? FindOfficialMap(
        IReadOnlyCollection<ParkOfficialMap> officialMaps,
        string? id,
        string? key,
        int? year,
        string? languageCode,
        ParkOfficialMapFormat? format)
    {
        string? requestedId = id ?? key;
        if (!string.IsNullOrWhiteSpace(requestedId))
        {
            ParkOfficialMap? byId = officialMaps.FirstOrDefault(officialMap => string.Equals(officialMap.Id, requestedId, StringComparison.OrdinalIgnoreCase));
            if (byId is not null)
            {
                return byId;
            }
        }

        if (!year.HasValue || !format.HasValue)
        {
            return null;
        }

        return officialMaps.FirstOrDefault(officialMap => officialMap.Year == year.Value
            && officialMap.Format == format.Value
            && string.Equals(NormalizeLanguageCode(officialMap.LanguageCode), languageCode, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> ValidateOfficialMap(string parkId, ParkOfficialMap officialMap)
    {
        List<string> errors = new List<string>();
        if (!IsStableOfficialMapIdentifier(officialMap.Id))
        {
            errors.Add("L'identifiant d'une carte officielle doit contenir entre 1 et 80 caractères alphanumériques, tirets ou underscores.");
        }

        int maximumYear = DateTime.UtcNow.Year + 1;
        if (officialMap.Year < 1800 || officialMap.Year > maximumYear)
        {
            errors.Add($"L'année '{officialMap.Year.ToString(CultureInfo.InvariantCulture)}' d'une carte officielle doit être comprise entre 1800 et {maximumYear.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (!Enum.IsDefined(officialMap.Format))
        {
            errors.Add($"Le format de la carte officielle {officialMap.Year.ToString(CultureInfo.InvariantCulture)} est invalide.");
        }

        bool hasExternalDocument = IsAbsoluteHttpUrl(officialMap.DocumentUrl);
        bool hasStoredDocument = !string.IsNullOrWhiteSpace(officialMap.StorageKey);
        if (!hasExternalDocument && !hasStoredDocument)
        {
            errors.Add($"documentUrl ou storageKey est obligatoire pour la carte officielle {officialMap.Year.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (!string.IsNullOrWhiteSpace(officialMap.DocumentUrl) && !hasExternalDocument)
        {
            errors.Add($"documentUrl doit être une URL HTTP(S) absolue pour la carte officielle {officialMap.Year.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (hasStoredDocument)
        {
            if (!ParkOfficialMapStorageKeys.BelongsTo(officialMap.StorageKey!, parkId, officialMap.Id))
            {
                errors.Add($"storageKey ne correspond pas au parc et à la carte officielle '{officialMap.Id}'.");
            }

            if (string.IsNullOrWhiteSpace(officialMap.OriginalFileName)
                || string.IsNullOrWhiteSpace(officialMap.ContentType)
                || !officialMap.SizeInBytes.HasValue
                || officialMap.SizeInBytes.Value <= 0)
            {
                errors.Add($"originalFileName, contentType et sizeInBytes sont obligatoires avec storageKey pour la carte officielle '{officialMap.Id}'.");
            }

            if (!string.IsNullOrWhiteSpace(officialMap.OriginalFileName) && officialMap.OriginalFileName.Length > 180)
            {
                errors.Add($"originalFileName dépasse 180 caractères pour la carte officielle '{officialMap.Id}'.");
            }

            if (!string.IsNullOrWhiteSpace(officialMap.ContentType)
                && !IsSupportedOfficialMapContentType(officialMap.Format, officialMap.ContentType))
            {
                errors.Add($"contentType ne correspond pas au format de la carte officielle '{officialMap.Id}'.");
            }

            if (officialMap.SizeInBytes > 25 * 1024 * 1024)
            {
                errors.Add($"sizeInBytes dépasse la limite de 25 Mo pour la carte officielle '{officialMap.Id}'.");
            }
        }

        if (!string.IsNullOrWhiteSpace(officialMap.PreviewImageUrl) && !IsAbsoluteHttpUrl(officialMap.PreviewImageUrl))
        {
            errors.Add($"previewImageUrl doit être une URL HTTP(S) absolue pour la carte officielle {officialMap.Year.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (!string.IsNullOrWhiteSpace(officialMap.SourcePageUrl) && !IsAbsoluteHttpUrl(officialMap.SourcePageUrl))
        {
            errors.Add($"sourcePageUrl doit être une URL HTTP(S) absolue pour la carte officielle {officialMap.Year.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (officialMap.IsVisible
            && officialMap.Format == ParkOfficialMapFormat.Image
            && !officialMap.AlternativeTexts.Any(static text => !string.IsNullOrWhiteSpace(text.LanguageCode) && !string.IsNullOrWhiteSpace(text.Value)))
        {
            errors.Add($"Une carte officielle image visible doit fournir au moins un texte alternatif localisé ({officialMap.Year.ToString(CultureInfo.InvariantCulture)}).");
        }

        return errors;
    }

    private static bool IsStableOfficialMapIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 80 || !IsAsciiLetterOrDigit(value[0]))
        {
            return false;
        }

        return value.All(static character => IsAsciiLetterOrDigit(character) || character is '-' or '_');
    }

    private static bool IsAsciiLetterOrDigit(char character)
    {
        return character is >= 'A' and <= 'Z'
            or >= 'a' and <= 'z'
            or >= '0' and <= '9';
    }

    private static bool IsSupportedOfficialMapContentType(ParkOfficialMapFormat format, string contentType)
    {
        string normalized = contentType.Trim().ToLowerInvariant();
        return format switch
        {
            ParkOfficialMapFormat.Image => normalized is "image/jpeg" or "image/png" or "image/webp" or "image/gif",
            ParkOfficialMapFormat.Pdf => normalized == "application/pdf",
            ParkOfficialMapFormat.Other => normalized is "application/vnd.google-earth.kml+xml"
                or "application/vnd.google-earth.kmz"
                or "application/zip",
            _ => false,
        };
    }

    private static bool IsAbsoluteHttpUrl(string? value)
    {
        return Uri.TryCreate(value?.Trim(), UriKind.Absolute, out Uri? uri)
            && (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildOfficialMapIdentity(ParkOfficialMap officialMap)
    {
        return $"{officialMap.Year.ToString(CultureInfo.InvariantCulture)}:{NormalizeLanguageCode(officialMap.LanguageCode) ?? "und"}:{officialMap.Format}";
    }

    private static string BuildOfficialMapDisplayName(ParkOfficialMap officialMap, int? requestedYear)
    {
        int year = requestedYear ?? officialMap.Year;
        string language = NormalizeLanguageCode(officialMap.LanguageCode) ?? "und";
        return $"Carte officielle {year.ToString(CultureInfo.InvariantCulture)} ({language}, {officialMap.Format})";
    }

    public static string Describe(IReadOnlyCollection<ParkOfficialMap> officialMaps)
    {
        return string.Join(", ", officialMaps
            .OrderByDescending(static officialMap => officialMap.Year)
            .ThenBy(static officialMap => officialMap.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static officialMap => officialMap.Format)
            .Select(static officialMap => BuildOfficialMapIdentity(officialMap)));
    }

    private static string ResolveOfficialMapMatchMode(ParkOfficialMap? existing, string? id, string? key)
    {
        if (existing is null)
        {
            return "none";
        }

        if (!string.IsNullOrWhiteSpace(id))
        {
            return "id";
        }

        if (!string.IsNullOrWhiteSpace(key) && string.Equals(existing.Id, key, StringComparison.OrdinalIgnoreCase))
        {
            return "key";
        }

        return "year+languageCode+format";
    }

    private static string? NormalizeLanguageCode(string? value)
    {
        string normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized.Length is > 0 and <= 16 ? normalized : null;
    }
}
