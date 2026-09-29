using System.Text.Json;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Core.Domain.Parks;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

internal static class ParkGraphUpsertProcessorStandaloneVisitorInformationExtensions
{
    internal static async Task<bool> ProcessStandaloneVisitorInformationAsync(
        this ParkGraphUpsertProcessor processorContext,
        JsonElement root,
        StandaloneAttraction? attraction,
        ParkGraphUpsertResult result,
        bool apply,
        CancellationToken cancellationToken)
    {
        if (!ParkGraphUpsertProcessorOpeningHoursExtensions.HasOpeningHoursPatch(root)
            && !ParkGraphUpsertProcessorPricingExtensions.HasPricingPatch(root))
        {
            return false;
        }

        if (attraction is null)
        {
            return false;
        }

        bool pricingChanged = await processorContext.ProcessStandalonePricingAsync(
            root,
            attraction,
            result,
            apply,
            cancellationToken);
        bool openingHoursChanged = await processorContext.ProcessStandaloneOpeningHoursAsync(
            root,
            attraction,
            result,
            apply,
            cancellationToken);
        return pricingChanged || openingHoursChanged;
    }

    private static async Task<bool> ProcessStandaloneOpeningHoursAsync(
        this ParkGraphUpsertProcessor processorContext,
        JsonElement root,
        StandaloneAttraction attraction,
        ParkGraphUpsertResult result,
        bool apply,
        CancellationToken cancellationToken)
    {
        if (!ParkGraphUpsertProcessorOpeningHoursExtensions.HasOpeningHoursPatch(root))
        {
            return false;
        }

        ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange(
            "StandaloneAttractionOpeningHours",
            attraction.Id,
            ParkGraphUpsertProcessor.OpeningHoursPropertyName,
            $"{attraction.Name} opening hours",
            "Unchanged",
            ParkGraphUpsertProcessor.OpeningHoursPropertyName);
        JsonElement? patch = ParkGraphUpsertProcessorOpeningHoursExtensions.ResolveOpeningHoursPatch(root);
        if (patch is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.Add("openingHours doit être un objet JSON.");
            return false;
        }

        if (!ParkGraphUpsertProcessorOpeningHoursExtensions.HasOpeningHoursScheduleData(patch.Value))
        {
            return false;
        }

        if (!ParkItemStatusNormalizer.IsOperating(attraction.AttractionDetails?.Status))
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.Add(
                "openingHours ne peut contenir des horaires actuels que pour une attraction autonome Operating.");
            return false;
        }

        if (processorContext.standaloneOpeningHoursRepository is null
            || processorContext.parkOpeningHoursScheduleNormalizer is null
            || processorContext.parkOpeningHoursCoverageSegmentBuilder is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.Add(
                "Le traitement des horaires d'attraction autonome n'est pas disponible dans ce contexte.");
            return false;
        }

        List<string> readErrors = new List<string>();
        ParkOpeningHoursSchedule schedule =
            ParkGraphUpsertProcessorOpeningHoursExtensions.ReadOpeningHoursSchedule(
                patch.Value,
                attraction.Id,
                readErrors);
        AddStandaloneTargetErrors(patch.Value, attraction.Id, "openingHours", readErrors);
        if (readErrors.Count > 0)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.AddRange(readErrors);
            return false;
        }

        ApplicationResult<ParkOpeningHoursSchedule> normalizedResult =
            processorContext.parkOpeningHoursScheduleNormalizer.Normalize(schedule);
        if (!normalizedResult.IsSuccess || normalizedResult.Value is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            ParkGraphUpsertProcessorOpeningHoursExtensions.AddOpeningHoursValidationErrors(
                result,
                normalizedResult);
            return false;
        }

        ParkOpeningHoursSchedule normalizedSchedule = normalizedResult.Value;
        ParkOpeningHoursSchedule? existing =
            await processorContext.standaloneOpeningHoursRepository
                .GetByStandaloneAttractionIdAsync(attraction.Id, cancellationToken);
        bool isNew = existing is null
            || !ParkGraphUpsertProcessorOpeningHoursExtensions.HasOpeningHoursData(existing);
        ParkGraphUpsertProcessorOpeningHoursExtensions.AddOpeningHoursChanges(
            change,
            existing,
            normalizedSchedule);
        if (change.Fields.Count > 0 || isNew)
        {
            change.ChangeType = isNew ? "Created" : "Updated";
        }

        result.Changes.Add(change);
        if (apply)
        {
            normalizedSchedule.CoverageSegments =
                processorContext.parkOpeningHoursCoverageSegmentBuilder
                    .BuildSegments(normalizedSchedule)
                    .ToList();
            await processorContext.standaloneOpeningHoursRepository.UpsertAsync(
                normalizedSchedule,
                cancellationToken);
        }

        return change.ChangeType is "Created" or "Updated";
    }

    private static async Task<bool> ProcessStandalonePricingAsync(
        this ParkGraphUpsertProcessor processorContext,
        JsonElement root,
        StandaloneAttraction attraction,
        ParkGraphUpsertResult result,
        bool apply,
        CancellationToken cancellationToken)
    {
        if (!ParkGraphUpsertProcessorPricingExtensions.HasPricingPatch(root))
        {
            return false;
        }

        ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange(
            "StandaloneAttractionPricing",
            attraction.Id,
            ParkGraphUpsertProcessor.PricingPropertyName,
            $"{attraction.Name} pricing",
            "Unchanged",
            ParkGraphUpsertProcessor.PricingPropertyName);
        JsonElement? patch = ParkGraphUpsertProcessorPricingExtensions.ResolvePricingPatch(root);
        if (patch is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.Add("pricing doit être un objet JSON.");
            return false;
        }

        if (!ParkGraphUpsertProcessorPricingExtensions.HasPricingData(patch.Value))
        {
            return false;
        }

        if (!ParkItemStatusNormalizer.IsOperating(attraction.AttractionDetails?.Status))
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.Add(
                "pricing ne peut contenir des tarifs actuels que pour une attraction autonome Operating.");
            return false;
        }

        if (processorContext.standalonePricingRepository is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.Add(
                "Le traitement des tarifs d'attraction autonome n'est pas disponible dans ce contexte.");
            return false;
        }

        List<string> readErrors = new List<string>();
        ParkPricingEntity pricing = ParkGraphUpsertProcessorPricingExtensions.ReadPricing(
            patch.Value,
            attraction.Id,
            readErrors);
        AddStandaloneTargetErrors(patch.Value, attraction.Id, "pricing", readErrors);
        ParkPricingEntity? existing =
            await processorContext.standalonePricingRepository
                .GetByStandaloneAttractionIdAsync(attraction.Id, cancellationToken);
        if (existing is not null)
        {
            if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(
                patch,
                "historicalSnapshots"))
            {
                pricing.HistoricalSnapshots = existing.HistoricalSnapshots;
            }

            if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "creditOffers"))
            {
                pricing.CreditOffers = existing.CreditOffers;
            }
        }

        if (readErrors.Count > 0)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.AddRange(readErrors);
            return false;
        }

        ApplicationResult<ParkPricingEntity> normalizedResult = ParkPricingNormalizer.Normalize(pricing);
        if (!normalizedResult.IsSuccess || normalizedResult.Value is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            ParkGraphUpsertProcessorPricingExtensions.AddPricingValidationErrors(
                result,
                normalizedResult);
            return false;
        }

        ParkPricingEntity normalizedPricing = normalizedResult.Value;
        bool isNew = existing is null || !ParkPricingNormalizer.HasPublicPricingData(existing);
        ParkGraphUpsertProcessorPricingExtensions.AddPricingChanges(
            change,
            existing,
            normalizedPricing);
        if (change.Fields.Count > 0 || isNew)
        {
            change.ChangeType = isNew ? "Created" : "Updated";
        }

        result.Changes.Add(change);
        if (apply)
        {
            await processorContext.standalonePricingRepository.UpsertAsync(
                normalizedPricing,
                cancellationToken);
        }

        return change.ChangeType is "Created" or "Updated";
    }

    private static void AddStandaloneTargetErrors(
        JsonElement patch,
        string targetId,
        string prefix,
        List<string> errors)
    {
        string? requestedId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(
            patch,
            "standaloneAttractionId");
        if (!string.IsNullOrWhiteSpace(requestedId)
            && !string.Equals(requestedId, targetId, StringComparison.Ordinal))
        {
            errors.Add(
                $"{prefix}.standaloneAttractionId pointe vers '{requestedId}' mais l'attraction cible est '{targetId}'.");
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "parkId"))
        {
            errors.Add(
                $"{prefix}.parkId n'est pas accepté pour une attraction autonome ; utilise standaloneAttractionId.");
        }
    }
}
