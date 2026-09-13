using System.Text.Json;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using System.Globalization;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Localization;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Core.Geo;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Parks.Services;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using System.Text;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Application.Features.Parks.Contracts;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;
internal static class ParkGraphUpsertProcessorReferencesExtensions
{
    internal static async Task ProcessFoundersAsync(this ParkGraphUpsertProcessor processorContext, JsonElement references, Dictionary<string, string> founderKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (!references.TryGetProperty("founders", out JsonElement founders) || founders.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        IReadOnlyCollection<ParkFounder> existingFounders = await processorContext.parkFounderRepository.GetAllAsync(cancellationToken);
        foreach (JsonElement patch in founders.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "key");
            string? id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "id");
            string? name = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "name");
            ParkFounder? entity = ParkGraphUpsertProcessorResolutionExtensions.FindByIdOrName(existingFounders, id, name, static value => value.Id, static value => value.Name);
            bool isNew = entity is null;
            entity ??= new ParkFounder
            {
                Name = name ?? string.Empty
            };
            ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("ParkFounder", entity.Id, key, entity.Name, isNew ? "Created" : "Unchanged", isNew ? "name" : ParkGraphUpsertProcessorJsonReadingExtensions.MatchMode(id, name));
            ParkGraphUpsertProcessorPatchingExtensions.PatchFounder(entity, patch, change);
            if (change.Fields.Count > 0 || isNew)
            {
                change.ChangeType = isNew ? "Created" : "Updated";
            }

            if (apply && (change.Fields.Count > 0 || isNew))
            {
                entity = isNew ? await processorContext.parkFounderRepository.CreateAsync(entity, cancellationToken) : await processorContext.parkFounderRepository.UpdateAsync(entity.Id, entity, cancellationToken) ?? entity;
                change.EntityId = entity.Id;
                await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Founders, entity.Id, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                founderKeys[key] = entity.Id;
            }

            result.Changes.Add(change);
        }
    }

    internal static async Task ProcessOperatorsAsync(this ParkGraphUpsertProcessor processorContext, JsonElement references, Dictionary<string, string> operatorKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (!references.TryGetProperty("operators", out JsonElement operators) || operators.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        IReadOnlyCollection<ParkOperator> existingOperators = await processorContext.parkOperatorRepository.GetAllAsync(cancellationToken);
        foreach (JsonElement patch in operators.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "key");
            string? id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "id");
            string? name = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "name");
            ParkOperator? entity = ParkGraphUpsertProcessorResolutionExtensions.FindByIdOrName(existingOperators, id, name, static value => value.Id, static value => value.Name);
            bool isNew = entity is null;
            entity ??= new ParkOperator
            {
                Name = name ?? string.Empty
            };
            ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("ParkOperator", entity.Id, key, entity.Name, isNew ? "Created" : "Unchanged", isNew ? "name" : ParkGraphUpsertProcessorJsonReadingExtensions.MatchMode(id, name));
            ParkGraphUpsertProcessorPatchingExtensions.PatchOperator(entity, patch, change);
            if (change.Fields.Count > 0 || isNew)
            {
                change.ChangeType = isNew ? "Created" : "Updated";
            }

            if (apply && (change.Fields.Count > 0 || isNew))
            {
                entity = isNew ? await processorContext.parkOperatorRepository.CreateAsync(entity, cancellationToken) : await processorContext.parkOperatorRepository.UpdateAsync(entity.Id, entity, cancellationToken) ?? entity;
                change.EntityId = entity.Id;
                await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Operators, entity.Id, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                operatorKeys[key] = entity.Id;
            }

            result.Changes.Add(change);
        }
    }

    internal static async Task ProcessManufacturersAsync(this ParkGraphUpsertProcessor processorContext, JsonElement references, Dictionary<string, string> manufacturerKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (!references.TryGetProperty("manufacturers", out JsonElement manufacturers) || manufacturers.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        IReadOnlyCollection<AttractionManufacturer> existingManufacturers = await processorContext.attractionManufacturerRepository.GetAllAsync(cancellationToken);
        foreach (JsonElement patch in manufacturers.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "key");
            string? id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "id");
            string? name = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "name");
            AttractionManufacturer? entity = ParkGraphUpsertProcessorResolutionExtensions.FindByIdOrName(existingManufacturers, id, name, static value => value.Id, static value => value.Name);
            bool isNew = entity is null;
            entity ??= new AttractionManufacturer
            {
                Name = name ?? string.Empty
            };
            ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("AttractionManufacturer", entity.Id, key, entity.Name, isNew ? "Created" : "Unchanged", isNew ? "name" : ParkGraphUpsertProcessorJsonReadingExtensions.MatchMode(id, name));
            ParkGraphUpsertProcessorPatchingExtensions.PatchManufacturer(entity, patch, change);
            if (change.Fields.Count > 0 || isNew)
            {
                change.ChangeType = isNew ? "Created" : "Updated";
            }

            if (apply && (change.Fields.Count > 0 || isNew))
            {
                entity = isNew ? await processorContext.attractionManufacturerRepository.CreateAsync(entity, cancellationToken) : await processorContext.attractionManufacturerRepository.UpdateAsync(entity.Id, entity, cancellationToken) ?? entity;
                change.EntityId = entity.Id;
                await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, entity.Id, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                manufacturerKeys[key] = entity.Id;
            }

            result.Changes.Add(change);
        }
    }
}
