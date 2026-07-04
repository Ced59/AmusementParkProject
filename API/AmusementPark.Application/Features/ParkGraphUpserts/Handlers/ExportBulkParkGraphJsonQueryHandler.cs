using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Handlers;

public sealed class ExportBulkParkGraphJsonQueryHandler : IQueryHandler<ExportBulkParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>>
{
    private static readonly JsonSerializerOptions ExportJsonOptions = BuildExportJsonOptions();
    private static readonly JsonWriterOptions ExportWriterOptions = new JsonWriterOptions { Indented = true };
    private static readonly IReadOnlySet<ParkGraphExportSection> ParkOnlySections = new HashSet<ParkGraphExportSection>
    {
        ParkGraphExportSection.ParkBasics,
        ParkGraphExportSection.ParkAudience,
        ParkGraphExportSection.ParkLocation,
        ParkGraphExportSection.ParkAdministration,
        ParkGraphExportSection.ParkDescriptions,
        ParkGraphExportSection.ParkHomeFeature,
    };

    private readonly IParkRepository parkRepository;
    private readonly IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<ParkListResult>>> parksPageHandler;
    private readonly IQueryHandler<SearchParksQuery, ApplicationResult<PagedResult<ParkListResult>>> searchParksHandler;
    private readonly BulkParkGraphJsonExportDataLoader graphDataLoader;

    public ExportBulkParkGraphJsonQueryHandler(
        IParkRepository parkRepository,
        IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<ParkListResult>>> parksPageHandler,
        IQueryHandler<SearchParksQuery, ApplicationResult<PagedResult<ParkListResult>>> searchParksHandler,
        BulkParkGraphJsonExportDataLoader graphDataLoader)
    {
        this.parkRepository = parkRepository;
        this.parksPageHandler = parksPageHandler;
        this.searchParksHandler = searchParksHandler;
        this.graphDataLoader = graphDataLoader;
    }

    public async Task<ApplicationResult<ParkGraphJsonExportResult>> HandleAsync(ExportBulkParkGraphJsonQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.Request);

        IReadOnlyCollection<Park> parks = await this.ResolveParksAsync(query.Request, cancellationToken);
        IReadOnlySet<ParkGraphExportSection> sections = query.Request.Sections.Distinct().ToHashSet();
        DateTime exportedAtUtc = DateTime.UtcNow;
        ReportProgress(query.Progress, "selection", 5, parks.Count, 0, "Sélection des parcs préparée.");
        await using MemoryStream output = new MemoryStream();
        await using (Utf8JsonWriter writer = new Utf8JsonWriter(output, ExportWriterOptions))
        {
            writer.WriteStartObject();
            writer.WriteString("documentType", "AmusementParkBulkParkGraphUpsert");
            writer.WriteString("schemaVersion", "2026-07-03");
            writer.WriteString("mode", "merge");
            WriteSelection(writer, query.Request, parks.Count);
            writer.WritePropertyName("parks");
            writer.WriteStartArray();

            if (CanUseParkOnlyExport(query.Request.Sections))
            {
                int processedParkCount = 0;
                ReportProgress(query.Progress, "writing", 10, parks.Count, processedParkCount, "Écriture du JSON bulk.");
                foreach (Park park in parks)
                {
                    WriteParkOnlyDocument(writer, park, query.Request.Sections, exportedAtUtc);
                    processedParkCount++;
                    ReportWritingProgress(query.Progress, parks.Count, processedParkCount, 10);
                }
            }
            else
            {
                ReportProgress(query.Progress, "loading-graph", 15, parks.Count, 0, "Chargement bulk des données liées.");
                BulkParkGraphJsonExportData graphData = await this.graphDataLoader.LoadAsync(parks, sections, cancellationToken);
                int processedParkCount = 0;
                ReportProgress(query.Progress, "writing", 40, parks.Count, processedParkCount, "Écriture du JSON bulk.");
                foreach (Park park in parks)
                {
                    string parkId = park.Id;
                    ParkGraphJsonParkExportData parkData = new ParkGraphJsonParkExportData
                    {
                        Park = park,
                        References = ResolveValue(graphData.ReferencesByParkId, parkId),
                        Zones = ResolveCollection(graphData.ZonesByParkId, parkId),
                        Items = ResolveCollection(graphData.ItemsByParkId, parkId),
                        Images = ResolveCollection(graphData.ImagesByParkId, parkId),
                        OpeningHours = ResolveValue(graphData.OpeningHoursByParkId, parkId),
                        HistoryEvents = ResolveCollection(graphData.HistoryEventsByParkId, parkId),
                    };
                    Dictionary<string, object?> document = ParkGraphJsonExportDocumentFactory.BuildDocument(
                        parkData,
                        sections,
                        exportedAtUtc,
                        "admin-bulk-park-graph-export");
                    JsonSerializer.Serialize(writer, document, ExportJsonOptions);
                    processedParkCount++;
                    ReportWritingProgress(query.Progress, parks.Count, processedParkCount, 40);
                }
            }

            writer.WriteEndArray();
            writer.WritePropertyName("metadata");
            writer.WriteStartObject();
            writer.WriteString("source", "admin-bulk-park-graph-export");
            writer.WriteString("exportedAtUtc", exportedAtUtc);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        ReportProgress(query.Progress, "completed", 100, parks.Count, parks.Count, "JSON bulk prêt.");
        return ApplicationResult<ParkGraphJsonExportResult>.Success(new ParkGraphJsonExportResult
        {
            FileName = BuildFileName(exportedAtUtc),
            Content = output.ToArray(),
        });
    }

    private async Task<IReadOnlyCollection<Park>> ResolveParksAsync(ParkGraphBulkExportRequest request, CancellationToken cancellationToken)
    {
        if (request.SelectionMode == ParkGraphBulkParkSelectionMode.Explicit)
        {
            List<string> normalizedIds = request.ParkIds
                .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
                .Select(static parkId => parkId.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();
            IReadOnlyCollection<Park> parks = await this.parkRepository.GetByIdsAsync(normalizedIds, cancellationToken);
            Dictionary<string, Park> parksById = parks
                .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
                .ToDictionary(static park => park.Id, StringComparer.Ordinal);
            return normalizedIds
                .Where(parksById.ContainsKey)
                .Select(parkId => parksById[parkId])
                .ToList();
        }

        PagedResult<ParkListResult> firstPage = await this.LoadPageAsync(request, 1, 1, cancellationToken);
        if (firstPage.TotalItems == 0)
        {
            return Array.Empty<Park>();
        }

        int pageSize = checked((int)Math.Min(firstPage.TotalItems, int.MaxValue));
        PagedResult<ParkListResult> allParks = await this.LoadPageAsync(request, 1, pageSize, cancellationToken);
        return allParks.Items
            .Select(static item => item.Park)
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .ToList();
    }

    private async Task<PagedResult<ParkListResult>> LoadPageAsync(ParkGraphBulkExportRequest request, int page, int pageSize, CancellationToken cancellationToken)
    {
        PagedQuery paging = new PagedQuery(page, pageSize);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            ApplicationResult<PagedResult<ParkListResult>> searchResult = await this.searchParksHandler.HandleAsync(
                new SearchParksQuery(
                    request.SearchTerm,
                    null,
                    paging,
                    IncludeHidden: true,
                    IsVisible: request.IsVisible,
                    AdminReviewStatus: request.AdminReviewStatus,
                    Type: request.Type,
                    AudienceClassificationFilter: request.AudienceClassificationFilter,
                    CountryCode: request.CountryCode,
                    HasValidCoordinates: request.HasValidCoordinates,
                    ClosedFilter: request.ClosedFilter,
                    OpeningHoursFilter: request.OpeningHoursFilter,
                    SortField: request.SortField,
                    SortDescending: request.SortDescending),
                cancellationToken);
            return ResolvePageResult(searchResult);
        }

        ApplicationResult<PagedResult<ParkListResult>> pageResult = await this.parksPageHandler.HandleAsync(
            new GetParksPageQuery(
                paging,
                IncludeHidden: true,
                IsVisible: request.IsVisible,
                AdminReviewStatus: request.AdminReviewStatus,
                Type: request.Type,
                AudienceClassificationFilter: request.AudienceClassificationFilter,
                CountryCode: request.CountryCode,
                HasValidCoordinates: request.HasValidCoordinates,
                ClosedFilter: request.ClosedFilter,
                OpeningHoursFilter: request.OpeningHoursFilter,
                SortField: request.SortField,
                SortDescending: request.SortDescending),
            cancellationToken);
        return ResolvePageResult(pageResult);
    }

    private static PagedResult<ParkListResult> ResolvePageResult(ApplicationResult<PagedResult<ParkListResult>> result)
    {
        if (!result.IsSuccess || result.Value is null)
        {
            return new PagedResult<ParkListResult>(new List<ParkListResult>(), 1, 1, 0);
        }

        return result.Value;
    }

    private static bool CanUseParkOnlyExport(IReadOnlyCollection<ParkGraphExportSection> sections)
    {
        return sections.All(ParkOnlySections.Contains);
    }

    private static void ReportWritingProgress(IProgress<ParkGraphJsonExportProgress>? progress, int exportedParkCount, int processedParkCount, int startPercentage)
    {
        if (progress is null)
        {
            return;
        }

        if (exportedParkCount > 20 && processedParkCount < exportedParkCount && processedParkCount % 25 != 0)
        {
            return;
        }

        int availablePercentage = 95 - startPercentage;
        int progressPercentage = exportedParkCount == 0
            ? 95
            : startPercentage + (int)Math.Floor(processedParkCount / (double)exportedParkCount * availablePercentage);
        ReportProgress(
            progress,
            "writing",
            Math.Min(95, progressPercentage),
            exportedParkCount,
            processedParkCount,
            "Écriture du JSON bulk.");
    }

    private static void ReportProgress(
        IProgress<ParkGraphJsonExportProgress>? progress,
        string step,
        int progressPercentage,
        int exportedParkCount,
        int processedParkCount,
        string message)
    {
        progress?.Report(new ParkGraphJsonExportProgress
        {
            Step = step,
            ProgressPercentage = Math.Max(0, Math.Min(100, progressPercentage)),
            ExportedParkCount = exportedParkCount,
            ProcessedParkCount = processedParkCount,
            Message = message,
        });
    }

    private static IReadOnlyCollection<TValue> ResolveCollection<TValue>(
        IReadOnlyDictionary<string, IReadOnlyCollection<TValue>> valuesByParkId,
        string key)
    {
        return valuesByParkId.TryGetValue(key, out IReadOnlyCollection<TValue>? values)
            ? values
            : Array.Empty<TValue>();
    }

    private static TValue? ResolveValue<TValue>(
        IReadOnlyDictionary<string, TValue?> valuesByParkId,
        string key)
        where TValue : class
    {
        return valuesByParkId.TryGetValue(key, out TValue? value)
            ? value
            : null;
    }

    private static void WriteSelection(Utf8JsonWriter writer, ParkGraphBulkExportRequest request, int exportedParkCount)
    {
        writer.WritePropertyName("selection");
        writer.WriteStartObject();
        writer.WriteString("mode", request.SelectionMode.ToString());
        writer.WriteNumber("exportedParkCount", exportedParkCount);
        writer.WritePropertyName("sections");
        writer.WriteStartArray();
        foreach (ParkGraphExportSection section in request.Sections)
        {
            writer.WriteStringValue(section.ToString());
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteParkOnlyDocument(Utf8JsonWriter writer, Park park, IReadOnlyCollection<ParkGraphExportSection> sections, DateTime exportedAtUtc)
    {
        writer.WriteStartObject();
        writer.WriteString("documentType", "AmusementParkParkGraphUpsert");
        writer.WriteString("schemaVersion", "2026-06-30");
        writer.WriteString("mode", "merge");
        WriteIdentity(writer, park);

        if (sections.Count > 0)
        {
            WriteParkPatch(writer, park, sections);
        }

        writer.WritePropertyName("metadata");
        writer.WriteStartObject();
        writer.WriteString("exportedAtUtc", exportedAtUtc);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteIdentity(Utf8JsonWriter writer, Park park)
    {
        writer.WritePropertyName("identity");
        writer.WriteStartObject();
        WriteNullableString(writer, "parkId", park.Id);
        WriteNullableString(writer, "id", park.Id);
        WriteNullableString(writer, "name", park.Name);
        WriteNullableString(writer, "countryCode", park.CountryCode);
        writer.WriteEndObject();
    }

    private static void WriteParkPatch(Utf8JsonWriter writer, Park park, IReadOnlyCollection<ParkGraphExportSection> sections)
    {
        bool includeBasics = sections.Contains(ParkGraphExportSection.ParkBasics);
        bool includeLocation = sections.Contains(ParkGraphExportSection.ParkLocation);

        writer.WritePropertyName("park");
        writer.WriteStartObject();

        if (includeBasics)
        {
            WriteNullableString(writer, "id", park.Id);
            WriteNullableString(writer, "name", park.Name);
        }

        if (includeBasics || includeLocation)
        {
            WriteNullableString(writer, "countryCode", park.CountryCode);
        }

        if (includeBasics)
        {
            WriteNullableEnum(writer, "type", park.Type);
            writer.WriteString("status", park.Status.ToString());
            WriteNullableDateTime(writer, "openingDate", park.OpeningDate);
            WriteNullableDateTime(writer, "closingDate", park.ClosingDate);
            WriteNullableString(writer, "openingDateText", park.OpeningDateText);
            WriteNullableString(writer, "closingDateText", park.ClosingDateText);
            WriteNullableString(writer, "founderId", park.FounderId);
            WriteNullableString(writer, "founderKey", park.FounderId);
            WriteNullableString(writer, "operatorId", park.OperatorId);
            WriteNullableString(writer, "operatorKey", park.OperatorId);
            WriteNullableString(writer, "websiteUrl", park.WebsiteUrl);
        }

        if (sections.Contains(ParkGraphExportSection.ParkAudience))
        {
            WriteNullableEnum(writer, "audienceClassification", park.AudienceClassification);
        }

        if (includeLocation)
        {
            WriteNullableString(writer, "street", park.Street);
            WriteNullableString(writer, "city", park.City);
            WriteNullableString(writer, "postalCode", park.PostalCode);
            WriteNullableDouble(writer, "latitude", park.Position?.Latitude);
            WriteNullableDouble(writer, "longitude", park.Position?.Longitude);
        }

        if (sections.Contains(ParkGraphExportSection.ParkAdministration))
        {
            writer.WriteBoolean("isVisible", park.IsVisible);
            writer.WriteString("adminReviewStatus", park.AdminReviewStatus.ToString());
        }

        if (sections.Contains(ParkGraphExportSection.ParkDescriptions))
        {
            writer.WritePropertyName("descriptions");
            JsonSerializer.Serialize(writer, park.Descriptions, ExportJsonOptions);
        }

        if (sections.Contains(ParkGraphExportSection.ParkHomeFeature))
        {
            writer.WriteBoolean("isFeaturedOnHome", park.IsFeaturedOnHome);
            WriteNullableInt32(writer, "featuredHomeOrder", park.FeaturedHomeOrder);
            writer.WriteBoolean("isFeaturedOnHomeSponsored", park.IsFeaturedOnHomeSponsored);
        }

        writer.WriteEndObject();
    }

    private static void WriteNullableString(Utf8JsonWriter writer, string propertyName, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(propertyName);
            return;
        }

        writer.WriteString(propertyName, value);
    }

    private static void WriteNullableEnum<TEnum>(Utf8JsonWriter writer, string propertyName, TEnum? value)
        where TEnum : struct, Enum
    {
        if (!value.HasValue)
        {
            writer.WriteNull(propertyName);
            return;
        }

        writer.WriteString(propertyName, value.Value.ToString());
    }

    private static void WriteNullableDateTime(Utf8JsonWriter writer, string propertyName, DateTime? value)
    {
        if (!value.HasValue)
        {
            writer.WriteNull(propertyName);
            return;
        }

        writer.WriteString(propertyName, value.Value);
    }

    private static void WriteNullableDouble(Utf8JsonWriter writer, string propertyName, double? value)
    {
        if (!value.HasValue)
        {
            writer.WriteNull(propertyName);
            return;
        }

        writer.WriteNumber(propertyName, value.Value);
    }

    private static void WriteNullableInt32(Utf8JsonWriter writer, string propertyName, int? value)
    {
        if (!value.HasValue)
        {
            writer.WriteNull(propertyName);
            return;
        }

        writer.WriteNumber(propertyName, value.Value);
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

    private static string BuildFileName(DateTime exportedAtUtc)
    {
        return $"bulk-parks-{exportedAtUtc.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}-park-graph.json";
    }
}
