using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ContextualBlocks.Contracts;
using AmusementPark.Application.Features.ContextualBlocks.Queries;
using AmusementPark.Application.Features.ContextualBlocks.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ContextualBlocks.Handlers;

public sealed class ExportContextualBlockJsonQueryHandler
    : IQueryHandler<ExportContextualBlockJsonQuery, ApplicationResult<ContextualBlockJsonExportResult>>
{
    private static readonly JsonSerializerOptions ExportJsonOptions = BuildExportJsonOptions();

    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public ExportContextualBlockJsonQueryHandler(IParkRepository parkRepository, IParkItemRepository parkItemRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<ApplicationResult<ContextualBlockJsonExportResult>> HandleAsync(ExportContextualBlockJsonQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.BlockType))
        {
            return ApplicationResult<ContextualBlockJsonExportResult>.Failure(ApplicationErrors.Required("blockType"));
        }

        if (string.IsNullOrWhiteSpace(query.EntityId))
        {
            return ApplicationResult<ContextualBlockJsonExportResult>.Failure(ApplicationErrors.Required("entityId"));
        }

        string blockType = query.BlockType.Trim();
        if (!ContextualBlockContracts.IsSupportedBlockType(blockType))
        {
            return ApplicationResult<ContextualBlockJsonExportResult>.Failure(ContextualBlockApplicationErrors.UnsupportedBlockType(blockType));
        }

        string entityId = query.EntityId.Trim();
        if (string.Equals(blockType, ContextualBlockContracts.ParkItemDescriptionBlockType, StringComparison.Ordinal))
        {
            ParkItem? item = await this.parkItemRepository.GetByIdAsync(entityId, true, cancellationToken);
            if (item is null)
            {
                return ApplicationResult<ContextualBlockJsonExportResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkItem), entityId));
            }

            DateTime parkItemExportedAtUtc = DateTime.UtcNow;
            ContextualParkItemDescriptionBlock itemBlock = new ContextualParkItemDescriptionBlock
            {
                ParkId = item.ParkId,
                ParkItemId = item.Id,
                ZoneId = item.ZoneId,
                Descriptions = BuildLocalizedDescriptions(item.Descriptions),
            };

            ContextualBlockExportDocument<ContextualParkItemDescriptionBlock> itemDocument = BuildDocument(
                item,
                blockType,
                BuildParkItemIds(item),
                itemBlock,
                parkItemExportedAtUtc);

            return ApplicationResult<ContextualBlockJsonExportResult>.Success(BuildResult(item, blockType, itemDocument, parkItemExportedAtUtc));
        }

        Park? park = await this.parkRepository.GetByIdAsync(entityId, true, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ContextualBlockJsonExportResult>.Failure(ApplicationErrors.EntityNotFound(nameof(Park), entityId));
        }

        DateTime exportedAtUtc = DateTime.UtcNow;
        if (string.Equals(blockType, ContextualBlockContracts.ParkDescriptionBlockType, StringComparison.Ordinal))
        {
            ContextualParkDescriptionBlock block = new ContextualParkDescriptionBlock
            {
                ParkId = park.Id,
                Descriptions = BuildLocalizedDescriptions(park),
            };

            ContextualBlockExportDocument<ContextualParkDescriptionBlock> document = BuildDocument(
                park,
                blockType,
                BuildParkIds(park),
                block,
                exportedAtUtc);

            return ApplicationResult<ContextualBlockJsonExportResult>.Success(BuildResult(park, blockType, document, exportedAtUtc));
        }

        ContextualParkPracticalBlock practicalBlock = new ContextualParkPracticalBlock
        {
            ParkId = park.Id,
            CountryCode = park.CountryCode,
            City = park.City,
            Street = park.Street,
            PostalCode = park.PostalCode,
            WebsiteUrl = park.WebsiteUrl,
            FounderId = park.FounderId,
            OperatorId = park.OperatorId,
            Latitude = park.Position?.Latitude,
            Longitude = park.Position?.Longitude,
        };

        ContextualBlockExportDocument<ContextualParkPracticalBlock> practicalDocument = BuildDocument(
            park,
            blockType,
            BuildPracticalIds(park),
            practicalBlock,
            exportedAtUtc);

        return ApplicationResult<ContextualBlockJsonExportResult>.Success(BuildResult(park, blockType, practicalDocument, exportedAtUtc));
    }

    private static JsonSerializerOptions BuildExportJsonOptions()
    {
        JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static ContextualBlockExportDocument<TBlock> BuildDocument<TBlock>(
        Park park,
        string blockType,
        Dictionary<string, string> ids,
        TBlock block,
        DateTime exportedAtUtc)
    {
        return new ContextualBlockExportDocument<TBlock>
        {
            BlockType = blockType,
            Target = new ContextualBlockExportTarget
            {
                EntityType = nameof(Park),
                EntityId = park.Id,
            },
            Ids = ids,
            Block = block,
            Metadata = new ContextualBlockExportMetadata
            {
                ExportedAtUtc = exportedAtUtc,
            },
        };
    }

    private static ContextualBlockExportDocument<TBlock> BuildDocument<TBlock>(
        ParkItem item,
        string blockType,
        Dictionary<string, string> ids,
        TBlock block,
        DateTime exportedAtUtc)
    {
        return new ContextualBlockExportDocument<TBlock>
        {
            BlockType = blockType,
            Target = new ContextualBlockExportTarget
            {
                EntityType = nameof(ParkItem),
                EntityId = item.Id,
            },
            Ids = ids,
            Block = block,
            Metadata = new ContextualBlockExportMetadata
            {
                ExportedAtUtc = exportedAtUtc,
            },
        };
    }

    private static ContextualBlockJsonExportResult BuildResult<TBlock>(
        Park park,
        string blockType,
        ContextualBlockExportDocument<TBlock> document,
        DateTime exportedAtUtc)
    {
        string json = JsonSerializer.Serialize(document, ExportJsonOptions);
        return new ContextualBlockJsonExportResult
        {
            FileName = BuildFileName(park, blockType, exportedAtUtc),
            Json = json,
        };
    }

    private static ContextualBlockJsonExportResult BuildResult<TBlock>(
        ParkItem item,
        string blockType,
        ContextualBlockExportDocument<TBlock> document,
        DateTime exportedAtUtc)
    {
        string json = JsonSerializer.Serialize(document, ExportJsonOptions);
        return new ContextualBlockJsonExportResult
        {
            FileName = BuildFileName(item, blockType, exportedAtUtc),
            Json = json,
        };
    }

    private static Dictionary<string, string> BuildParkIds(Park park)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["parkId"] = park.Id,
        };
    }

    private static Dictionary<string, string> BuildPracticalIds(Park park)
    {
        Dictionary<string, string> ids = BuildParkIds(park);
        AddOptionalId(ids, "founderId", park.FounderId);
        AddOptionalId(ids, "operatorId", park.OperatorId);
        return ids;
    }

    private static Dictionary<string, string> BuildParkItemIds(ParkItem item)
    {
        Dictionary<string, string> ids = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["parkId"] = item.ParkId,
            ["parkItemId"] = item.Id,
        };
        AddOptionalId(ids, "zoneId", item.ZoneId);
        return ids;
    }

    private static void AddOptionalId(Dictionary<string, string> ids, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        ids[key] = value.Trim();
    }

    private static List<LocalizedText> BuildLocalizedDescriptions(Park park)
    {
        return BuildLocalizedDescriptions(park.Descriptions);
    }

    private static List<LocalizedText> BuildLocalizedDescriptions(IReadOnlyCollection<LocalizedText> sourceDescriptions)
    {
        Dictionary<string, LocalizedText> descriptionsByLanguage = new Dictionary<string, LocalizedText>(StringComparer.OrdinalIgnoreCase);
        foreach (LocalizedText description in sourceDescriptions)
        {
            string languageCode = NormalizeLanguageCode(description.LanguageCode);
            if (languageCode.Length == 0 || descriptionsByLanguage.ContainsKey(languageCode))
            {
                continue;
            }

            descriptionsByLanguage[languageCode] = description;
        }

        List<LocalizedText> descriptions = new List<LocalizedText>();
        foreach (string languageCode in ContextualBlockContracts.SupportedLanguageCodes)
        {
            LocalizedText? description;
            if (descriptionsByLanguage.TryGetValue(languageCode, out description))
            {
                descriptions.Add(new LocalizedText(languageCode, description.Value));
            }
            else
            {
                descriptions.Add(new LocalizedText(languageCode, null));
            }
        }

        return descriptions;
    }

    private static string NormalizeLanguageCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    private static string BuildFileName(Park park, string blockType, DateTime exportedAtUtc)
    {
        string sourceName = string.IsNullOrWhiteSpace(park.Name) ? park.Id : park.Name;
        string safeName = SanitizeFileName(sourceName);
        string safeBlockType = blockType.Replace('.', '-');
        return $"{safeName}-{exportedAtUtc.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}-{safeBlockType}-contextual-block.json";
    }

    private static string BuildFileName(ParkItem item, string blockType, DateTime exportedAtUtc)
    {
        string sourceName = string.IsNullOrWhiteSpace(item.Name) ? item.Id : item.Name;
        string safeName = SanitizeFileName(sourceName);
        string safeBlockType = blockType.Replace('.', '-');
        return $"{safeName}-{exportedAtUtc.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}-{safeBlockType}-contextual-block.json";
    }

    private static string SanitizeFileName(string value)
    {
        StringBuilder builder = new StringBuilder();
        foreach (char character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (character == '-' || character == '_')
            {
                builder.Append(character);
            }
            else if (char.IsWhiteSpace(character))
            {
                builder.Append('-');
            }
        }

        string result = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(result) ? "park" : result;
    }
}
