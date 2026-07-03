using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Handlers;

public sealed class ExportBulkParkGraphJsonQueryHandler : IQueryHandler<ExportBulkParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>>
{
    private static readonly JsonSerializerOptions ExportJsonOptions = BuildExportJsonOptions();

    private readonly IParkRepository parkRepository;
    private readonly IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<ParkListResult>>> parksPageHandler;
    private readonly IQueryHandler<SearchParksQuery, ApplicationResult<PagedResult<ParkListResult>>> searchParksHandler;
    private readonly IQueryHandler<ExportParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>> parkExportHandler;

    public ExportBulkParkGraphJsonQueryHandler(
        IParkRepository parkRepository,
        IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<ParkListResult>>> parksPageHandler,
        IQueryHandler<SearchParksQuery, ApplicationResult<PagedResult<ParkListResult>>> searchParksHandler,
        IQueryHandler<ExportParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>> parkExportHandler)
    {
        this.parkRepository = parkRepository;
        this.parksPageHandler = parksPageHandler;
        this.searchParksHandler = searchParksHandler;
        this.parkExportHandler = parkExportHandler;
    }

    public async Task<ApplicationResult<ParkGraphJsonExportResult>> HandleAsync(ExportBulkParkGraphJsonQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.Request);

        IReadOnlyCollection<Park> parks = await this.ResolveParksAsync(query.Request, cancellationToken);
        JsonArray parkDocuments = new JsonArray();
        foreach (Park park in parks)
        {
            ApplicationResult<ParkGraphJsonExportResult> exportResult = await this.parkExportHandler.HandleAsync(
                new ExportParkGraphJsonQuery(park.Id, query.Request.Sections),
                cancellationToken);
            if (!exportResult.IsSuccess || exportResult.Value is null)
            {
                return ApplicationResult<ParkGraphJsonExportResult>.Failure(exportResult.Errors);
            }

            JsonNode? document = JsonNode.Parse(exportResult.Value.Json);
            if (document is not null)
            {
                parkDocuments.Add(document);
            }
        }

        DateTime exportedAtUtc = DateTime.UtcNow;
        JsonObject root = new JsonObject
        {
            ["documentType"] = "AmusementParkBulkParkGraphUpsert",
            ["schemaVersion"] = "2026-07-03",
            ["mode"] = "merge",
            ["selection"] = BuildSelectionNode(query.Request, parks.Count),
            ["parks"] = parkDocuments,
            ["metadata"] = new JsonObject
            {
                ["source"] = "admin-bulk-park-graph-export",
                ["exportedAtUtc"] = exportedAtUtc,
            },
        };

        byte[] content = JsonSerializer.SerializeToUtf8Bytes(root, ExportJsonOptions);
        return ApplicationResult<ParkGraphJsonExportResult>.Success(new ParkGraphJsonExportResult
        {
            FileName = BuildFileName(exportedAtUtc),
            Content = content,
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

    private static JsonObject BuildSelectionNode(ParkGraphBulkExportRequest request, int exportedParkCount)
    {
        JsonArray sections = new JsonArray();
        foreach (ParkGraphExportSection section in request.Sections)
        {
            sections.Add(section.ToString());
        }

        return new JsonObject
        {
            ["mode"] = request.SelectionMode.ToString(),
            ["exportedParkCount"] = exportedParkCount,
            ["sections"] = sections,
        };
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
