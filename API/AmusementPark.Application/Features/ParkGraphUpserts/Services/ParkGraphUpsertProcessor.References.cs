using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.Search;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private async Task ProcessFoundersAsync(JsonElement references, Dictionary<string, string> founderKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (!references.TryGetProperty("founders", out JsonElement founders) || founders.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        IReadOnlyCollection<ParkFounder> existingFounders = await this.parkFounderRepository.GetAllAsync(cancellationToken);
        foreach (JsonElement patch in founders.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ReadString(patch, "key");
            string? id = ReadString(patch, "id");
            string? name = ReadString(patch, "name");
            ParkFounder? entity = FindByIdOrName(existingFounders, id, name, static value => value.Id, static value => value.Name);
            bool isNew = entity is null;
            entity ??= new ParkFounder { Name = name ?? string.Empty };
            ParkGraphUpsertChange change = BuildEntityChange("ParkFounder", entity.Id, key, entity.Name, isNew ? "Created" : "Unchanged", isNew ? "name" : MatchMode(id, name));
            PatchFounder(entity, patch, change);

            if (change.Fields.Count > 0 || isNew)
            {
                change.ChangeType = isNew ? "Created" : "Updated";
            }

            if (apply && (change.Fields.Count > 0 || isNew))
            {
                entity = isNew
                    ? await this.parkFounderRepository.CreateAsync(entity, cancellationToken)
                    : await this.parkFounderRepository.UpdateAsync(entity.Id, entity, cancellationToken) ?? entity;
                change.EntityId = entity.Id;
                await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Founders, entity.Id, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                founderKeys[key] = entity.Id;
            }

            result.Changes.Add(change);
        }
    }

    private async Task ProcessOperatorsAsync(JsonElement references, Dictionary<string, string> operatorKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (!references.TryGetProperty("operators", out JsonElement operators) || operators.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        IReadOnlyCollection<ParkOperator> existingOperators = await this.parkOperatorRepository.GetAllAsync(cancellationToken);
        foreach (JsonElement patch in operators.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ReadString(patch, "key");
            string? id = ReadString(patch, "id");
            string? name = ReadString(patch, "name");
            ParkOperator? entity = FindByIdOrName(existingOperators, id, name, static value => value.Id, static value => value.Name);
            bool isNew = entity is null;
            entity ??= new ParkOperator { Name = name ?? string.Empty };
            ParkGraphUpsertChange change = BuildEntityChange("ParkOperator", entity.Id, key, entity.Name, isNew ? "Created" : "Unchanged", isNew ? "name" : MatchMode(id, name));
            PatchOperator(entity, patch, change);

            if (change.Fields.Count > 0 || isNew)
            {
                change.ChangeType = isNew ? "Created" : "Updated";
            }

            if (apply && (change.Fields.Count > 0 || isNew))
            {
                entity = isNew
                    ? await this.parkOperatorRepository.CreateAsync(entity, cancellationToken)
                    : await this.parkOperatorRepository.UpdateAsync(entity.Id, entity, cancellationToken) ?? entity;
                change.EntityId = entity.Id;
                await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Operators, entity.Id, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                operatorKeys[key] = entity.Id;
            }

            result.Changes.Add(change);
        }
    }

    private async Task ProcessManufacturersAsync(JsonElement references, Dictionary<string, string> manufacturerKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (!references.TryGetProperty("manufacturers", out JsonElement manufacturers) || manufacturers.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        IReadOnlyCollection<AttractionManufacturer> existingManufacturers = await this.attractionManufacturerRepository.GetAllAsync(cancellationToken);
        foreach (JsonElement patch in manufacturers.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ReadString(patch, "key");
            string? id = ReadString(patch, "id");
            string? name = ReadString(patch, "name");
            AttractionManufacturer? entity = FindByIdOrName(existingManufacturers, id, name, static value => value.Id, static value => value.Name);
            bool isNew = entity is null;
            entity ??= new AttractionManufacturer { Name = name ?? string.Empty };
            ParkGraphUpsertChange change = BuildEntityChange("AttractionManufacturer", entity.Id, key, entity.Name, isNew ? "Created" : "Unchanged", isNew ? "name" : MatchMode(id, name));
            PatchManufacturer(entity, patch, change);

            if (change.Fields.Count > 0 || isNew)
            {
                change.ChangeType = isNew ? "Created" : "Updated";
            }

            if (apply && (change.Fields.Count > 0 || isNew))
            {
                entity = isNew
                    ? await this.attractionManufacturerRepository.CreateAsync(entity, cancellationToken)
                    : await this.attractionManufacturerRepository.UpdateAsync(entity.Id, entity, cancellationToken) ?? entity;
                change.EntityId = entity.Id;
                await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, entity.Id, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                manufacturerKeys[key] = entity.Id;
            }

            result.Changes.Add(change);
        }
    }
}
