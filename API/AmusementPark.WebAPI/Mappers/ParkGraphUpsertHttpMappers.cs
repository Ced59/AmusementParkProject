using System.Text.Json;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.ParkGraphUpserts;

namespace AmusementPark.WebAPI.Mappers;

internal static class ParkGraphUpsertHttpMappers
{
    public static ParkGraphUpsertRequest ToApplication(this ParkGraphUpsertRequestDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string rawJson = dto.Document.ValueKind == JsonValueKind.Undefined
            ? string.Empty
            : dto.Document.GetRawText();

        return new ParkGraphUpsertRequest
        {
            TargetParkId = dto.TargetParkId,
            CreateIfMissing = dto.CreateIfMissing,
            ReplaceCollections = dto.ReplaceCollections,
            Document = dto.Document,
            RawJson = rawJson,
        };
    }

    public static ParkGraphBulkExportRequest ToApplication(this ParkGraphBulkExportRequestDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ParkGraphBulkExportRequest
        {
            SelectionMode = ParseSelectionMode(dto.SelectionMode),
            ParkIds = dto.ParkIds,
            SearchTerm = dto.SearchTerm,
            IsVisible = dto.IsVisible,
            AdminReviewStatus = ParseEnum<AdminReviewStatus>(dto.AdminReviewStatus),
            Type = ParseEnum<ParkType>(dto.Type),
            AudienceClassificationFilter = ParkAudienceClassificationFilterParser.Parse(dto.AudienceClassification),
            CountryCode = dto.CountryCode,
            HasValidCoordinates = dto.HasValidCoordinates,
            ClosedFilter = ParseClosedEntityFilter(dto.ClosedFilter),
            OpeningHoursFilter = ParseOpeningHoursFilter(dto.OpeningHoursStatus),
            SortField = ParseParkAdminSortField(dto.SortBy),
            SortDescending = IsDescendingSort(dto.SortDirection),
            Sections = ParseExportSections(dto.Sections),
        };
    }

    public static BulkParkGraphUpsertRequest ToApplication(this BulkParkGraphUpsertRequestDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string rawJson = dto.Document.ValueKind == JsonValueKind.Undefined
            ? string.Empty
            : dto.Document.GetRawText();

        return new BulkParkGraphUpsertRequest
        {
            CreateIfMissing = dto.CreateIfMissing,
            ReplaceCollections = dto.ReplaceCollections,
            Document = dto.Document,
            RawJson = rawJson,
        };
    }

    public static ParkGraphUpsertResultDto ToHttp(this ParkGraphUpsertResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ParkGraphUpsertResultDto
        {
            OperationId = result.OperationId,
            Mode = result.Mode,
            IsApplied = result.IsApplied,
            CanApply = result.CanApply,
            PreviewedAtUtc = result.PreviewedAtUtc,
            AppliedAtUtc = result.AppliedAtUtc,
            TargetParkId = result.TargetParkId,
            TargetParkName = result.TargetParkName,
            TargetStandaloneAttractionId = result.TargetStandaloneAttractionId,
            TargetStandaloneAttractionName = result.TargetStandaloneAttractionName,
            Counts = new ParkGraphUpsertCountsDto
            {
                Created = result.Counts.Created,
                Updated = result.Counts.Updated,
                Deleted = result.Counts.Deleted,
                Unchanged = result.Counts.Unchanged,
                Warnings = result.Counts.Warnings,
                Errors = result.Counts.Errors,
            },
            Changes = result.Changes.Select(static change => new ParkGraphUpsertChangeDto
            {
                EntityType = change.EntityType,
                EntityId = change.EntityId,
                EntityKey = change.EntityKey,
                DisplayName = change.DisplayName,
                ChangeType = change.ChangeType,
                MatchedBy = change.MatchedBy,
                Fields = change.Fields.Select(static field => new ParkGraphUpsertFieldChangeDto
                {
                    Field = field.Field,
                    OldValue = field.OldValue,
                    NewValue = field.NewValue,
                }).ToList(),
            }).ToList(),
            Warnings = result.Warnings.ToList(),
            Errors = result.Errors.ToList(),
        };
    }

    public static BulkParkGraphUpsertResultDto ToHttp(this BulkParkGraphUpsertResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new BulkParkGraphUpsertResultDto
        {
            OperationId = result.OperationId,
            IsApplied = result.IsApplied,
            CanApply = result.CanApply,
            PreviewedAtUtc = result.PreviewedAtUtc,
            AppliedAtUtc = result.AppliedAtUtc,
            Counts = new ParkGraphUpsertCountsDto
            {
                Created = result.Counts.Created,
                Updated = result.Counts.Updated,
                Deleted = result.Counts.Deleted,
                Unchanged = result.Counts.Unchanged,
                Warnings = result.Counts.Warnings,
                Errors = result.Counts.Errors,
            },
            Parks = result.Parks.Select(static park => new BulkParkGraphUpsertParkResultDto
            {
                Index = park.Index,
                TargetParkId = park.TargetParkId,
                TargetParkName = park.TargetParkName,
                Result = park.Result.ToHttp(),
            }).ToList(),
            Warnings = result.Warnings.ToList(),
            Errors = result.Errors.ToList(),
        };
    }

    public static ParkGraphUpsertHistoryEntryDto ToHttp(this ParkGraphUpsertHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new ParkGraphUpsertHistoryEntryDto
        {
            Id = entry.Id,
            OperationKind = entry.OperationKind,
            TargetParkId = entry.TargetParkId,
            TargetParkName = entry.TargetParkName,
            RequestedByUserId = entry.RequestedByUserId,
            CreatedAtUtc = entry.CreatedAtUtc,
            RawJson = entry.RawJson,
            Result = entry.Result.ToHttp(),
        };
    }

    private static ParkGraphBulkParkSelectionMode ParseSelectionMode(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "explicit" or "selected" or "selection" => ParkGraphBulkParkSelectionMode.Explicit,
            _ => ParkGraphBulkParkSelectionMode.Filtered,
        };
    }

    private static IReadOnlyCollection<ParkGraphExportSection> ParseExportSections(IReadOnlyCollection<string> values)
    {
        return values
            .Select(static value => ParseEnum<ParkGraphExportSection>(value))
            .Where(static value => value.HasValue)
            .Select(static value => value!.Value)
            .Distinct()
            .ToList();
    }

    private static ClosedEntityFilter ParseClosedEntityFilter(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "openonly" or "open" => ClosedEntityFilter.OpenOnly,
            "closedonly" or "closed" => ClosedEntityFilter.ClosedOnly,
            _ => ClosedEntityFilter.All,
        };
    }

    private static ParkOpeningHoursAdminFilter ParseOpeningHoursFilter(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "configured" => ParkOpeningHoursAdminFilter.Configured,
            "notconfigured" => ParkOpeningHoursAdminFilter.NotConfigured,
            "uptodate" => ParkOpeningHoursAdminFilter.UpToDate,
            "needsupdate" => ParkOpeningHoursAdminFilter.NeedsUpdate,
            "expired" => ParkOpeningHoursAdminFilter.Expired,
            _ => ParkOpeningHoursAdminFilter.All,
        };
    }

    private static ParkAdminSortField ParseParkAdminSortField(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "name" => ParkAdminSortField.Name,
            "parkitemstotalcount" => ParkAdminSortField.ParkItemsTotalCount,
            "parkitemsvisiblecount" => ParkAdminSortField.ParkItemsVisibleCount,
            "openinghoursstatus" => ParkAdminSortField.OpeningHoursStatus,
            "datacompletenessscore" or "datacompleteness" or "completeness" or "completenessscore" => ParkAdminSortField.DataCompletenessScore,
            _ => ParkAdminSortField.Default,
        };
    }

    private static bool IsDescendingSort(string? value)
    {
        return value?.Trim().ToLowerInvariant() is "desc" or "descending" or "-1";
    }

    private static T? ParseEnum<T>(string? value)
        where T : struct
    {
        return Enum.TryParse(value, true, out T parsed) ? parsed : null;
    }
}
