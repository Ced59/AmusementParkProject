using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
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
            Counts = new ParkGraphUpsertCountsDto
            {
                Created = result.Counts.Created,
                Updated = result.Counts.Updated,
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
}
