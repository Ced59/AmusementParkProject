using System.Text.Json;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed class BulkParkGraphUpsertProcessor
{
    private readonly ParkGraphUpsertProcessor processor;

    public BulkParkGraphUpsertProcessor(ParkGraphUpsertProcessor processor)
    {
        this.processor = processor;
    }

    public async Task<ApplicationResult<BulkParkGraphUpsertResult>> ProcessAsync(
        BulkParkGraphUpsertRequest request,
        string? requestedByUserId,
        bool apply,
        CancellationToken cancellationToken)
    {
        if (request.Document.ValueKind != JsonValueKind.Object)
        {
            return ApplicationResult<BulkParkGraphUpsertResult>.Failure(ParkGraphUpsertApplicationErrors.InvalidDocument("Le document JSON bulk racine doit être un objet."));
        }

        JsonElement root = request.Document;
        JsonElement? parks = ResolveParksArray(root);
        if (parks is null)
        {
            return ApplicationResult<BulkParkGraphUpsertResult>.Failure(ParkGraphUpsertApplicationErrors.InvalidDocument("Le document JSON bulk doit contenir un tableau parks."));
        }

        if (request.CreateIfMissing)
        {
            return ApplicationResult<BulkParkGraphUpsertResult>.Failure(ParkGraphUpsertApplicationErrors.InvalidDocument("Le mode bulk est limité aux mises à jour de parcs existants : createIfMissing doit rester désactivé."));
        }

        BulkParkGraphUpsertResult aggregate = new BulkParkGraphUpsertResult
        {
            IsApplied = apply,
            AppliedAtUtc = apply ? DateTime.UtcNow : null,
        };

        int index = 0;
        foreach (JsonElement parkDocument in parks.Value.EnumerateArray())
        {
            BulkParkGraphUpsertParkResult parkResult = await this.ProcessParkAsync(
                parkDocument,
                index,
                request,
                requestedByUserId,
                apply,
                cancellationToken);
            aggregate.Parks.Add(parkResult);
            MergeResult(aggregate, parkResult.Result);
            index++;
        }

        if (aggregate.Parks.Count == 0)
        {
            aggregate.CanApply = false;
            aggregate.Errors.Add("Le document bulk ne contient aucun parc.");
        }

        aggregate.CanApply = aggregate.CanApply
            && aggregate.Errors.Count == 0
            && aggregate.Parks.All(static park => park.Result.CanApply && park.Result.Errors.Count == 0);
        aggregate.Counts.Warnings = aggregate.Warnings.Count;
        aggregate.Counts.Errors = aggregate.Errors.Count;

        return ApplicationResult<BulkParkGraphUpsertResult>.Success(aggregate);
    }

    private async Task<BulkParkGraphUpsertParkResult> ProcessParkAsync(
        JsonElement parkDocument,
        int index,
        BulkParkGraphUpsertRequest request,
        string? requestedByUserId,
        bool apply,
        CancellationToken cancellationToken)
    {
        if (parkDocument.ValueKind != JsonValueKind.Object)
        {
            ParkGraphUpsertResult invalidResult = BuildInvalidParkResult(index, apply, "Chaque entrée parks doit être un objet JSON.");
            return new BulkParkGraphUpsertParkResult
            {
                Index = index,
                Result = invalidResult,
            };
        }

        string? targetParkId = ResolveTargetParkId(parkDocument);
        ParkGraphUpsertRequest singleRequest = new ParkGraphUpsertRequest
        {
            TargetParkId = targetParkId,
            CreateIfMissing = request.CreateIfMissing,
            ReplaceCollections = request.ReplaceCollections,
            Document = parkDocument,
            RawJson = parkDocument.GetRawText(),
        };

        ApplicationResult<ParkGraphUpsertResult> result;
        if (apply)
        {
            ApplicationResult<ParkGraphUpsertResult> previewResult = await this.processor.PreviewAsync(singleRequest, requestedByUserId, cancellationToken);
            ParkGraphUpsertResult previewItemResult = previewResult.Value ?? BuildFailureResult(previewResult.Errors, false);
            RejectCreatedChanges(previewItemResult, index);
            if (!previewItemResult.CanApply || previewItemResult.Errors.Count > 0)
            {
                return new BulkParkGraphUpsertParkResult
                {
                    Index = index,
                    TargetParkId = previewItemResult.TargetParkId ?? targetParkId,
                    TargetParkName = previewItemResult.TargetParkName ?? ResolveTargetParkName(parkDocument),
                    Result = previewItemResult,
                };
            }

            result = await this.processor.ApplyAsync(singleRequest, requestedByUserId, cancellationToken);
        }
        else
        {
            result = await this.processor.PreviewAsync(singleRequest, requestedByUserId, cancellationToken);
        }

        ParkGraphUpsertResult itemResult = result.Value ?? BuildFailureResult(result.Errors, apply);
        RejectCreatedChanges(itemResult, index);

        return new BulkParkGraphUpsertParkResult
        {
            Index = index,
            TargetParkId = itemResult.TargetParkId ?? targetParkId,
            TargetParkName = itemResult.TargetParkName ?? ResolveTargetParkName(parkDocument),
            Result = itemResult,
        };
    }

    private static void RejectCreatedChanges(ParkGraphUpsertResult result, int index)
    {
        List<ParkGraphUpsertChange> createdChanges = result.Changes
            .Where(static change => string.Equals(change.ChangeType, "Created", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (createdChanges.Count == 0)
        {
            return;
        }

        foreach (ParkGraphUpsertChange change in createdChanges)
        {
            result.Errors.Add($"parks[{index}] : le mode bulk update-only interdit la création de {change.EntityType} '{change.DisplayName}'. Ne conserver que les entités et propriétés présentes dans l'export.");
        }

        result.CanApply = false;
        result.Counts.Errors = result.Errors.Count;
    }

    private static JsonElement? ResolveParksArray(JsonElement root)
    {
        if (root.TryGetProperty("parks", out JsonElement parks) && parks.ValueKind == JsonValueKind.Array)
        {
            return parks;
        }

        return null;
    }

    private static string? ResolveTargetParkId(JsonElement root)
    {
        JsonElement? identity = GetObject(root, "identity");
        JsonElement? park = GetObject(root, "park");
        JsonElement? openingHours = GetObject(root, "openingHours");

        return ReadString(identity, "parkId")
            ?? ReadString(identity, "id")
            ?? ReadString(park, "id")
            ?? ReadString(openingHours, "parkId");
    }

    private static string? ResolveTargetParkName(JsonElement root)
    {
        JsonElement? identity = GetObject(root, "identity");
        JsonElement? park = GetObject(root, "park");
        return ReadString(identity, "name") ?? ReadString(park, "name");
    }

    private static JsonElement? GetObject(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind == JsonValueKind.Object)
        {
            return property;
        }

        return null;
    }

    private static string? ReadString(JsonElement? element, string propertyName)
    {
        if (element is null
            || element.Value.ValueKind != JsonValueKind.Object
            || !element.Value.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        string? value = property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.ToString();
        string? trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static ParkGraphUpsertResult BuildInvalidParkResult(int index, bool apply, string message)
    {
        return new ParkGraphUpsertResult
        {
            IsApplied = apply,
            AppliedAtUtc = apply ? DateTime.UtcNow : null,
            CanApply = false,
            Errors = new List<string>
            {
                $"parks[{index}] : {message}",
            },
            Counts = new ParkGraphUpsertCounts
            {
                Errors = 1,
            },
        };
    }

    private static ParkGraphUpsertResult BuildFailureResult(IReadOnlyCollection<ApplicationError> errors, bool apply)
    {
        List<string> messages = errors.Select(static error => error.Message).ToList();
        return new ParkGraphUpsertResult
        {
            IsApplied = apply,
            AppliedAtUtc = apply ? DateTime.UtcNow : null,
            CanApply = false,
            Errors = messages,
            Counts = new ParkGraphUpsertCounts
            {
                Errors = messages.Count,
            },
        };
    }

    private static void MergeResult(BulkParkGraphUpsertResult aggregate, ParkGraphUpsertResult result)
    {
        aggregate.Counts.Created += result.Counts.Created;
        aggregate.Counts.Updated += result.Counts.Updated;
        aggregate.Counts.Deleted += result.Counts.Deleted;
        aggregate.Counts.Unchanged += result.Counts.Unchanged;

        foreach (string warning in result.Warnings)
        {
            aggregate.Warnings.Add(warning);
        }

        foreach (string error in result.Errors)
        {
            aggregate.Errors.Add(error);
        }
    }
}
