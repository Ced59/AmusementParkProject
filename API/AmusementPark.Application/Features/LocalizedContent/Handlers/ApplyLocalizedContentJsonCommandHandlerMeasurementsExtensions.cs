using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.AttractionAccessConditionTypes;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Contracts;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Ports;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.LocalizedContent.Commands;
using AmusementPark.Application.Features.LocalizedContent.Results;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;
using AmusementPark.Application.Common.Measurements;

namespace AmusementPark.Application.Features.LocalizedContent.Handlers;
internal static class ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions
{
    internal static void NormalizeAttractionDetailsAfterPatch(this ApplyLocalizedContentJsonCommandHandler processorContext, AttractionDetails details, List<string> updatedFields)
    {
        double? previousHeightInMeters = details.HeightInMeters;
        double? previousHeightInFeet = details.HeightInFeet;
        double? previousLengthInMeters = details.LengthInMeters;
        double? previousLengthInFeet = details.LengthInFeet;
        double? previousSpeedInKmH = details.SpeedInKmH;
        double? previousSpeedInMph = details.SpeedInMph;
        double? previousDropInMeters = details.DropInMeters;
        double? previousDropInFeet = details.DropInFeet;
        List<(double? Value, AttractionAccessConditionUnit? Unit)> previousAccessConditions = details.AccessConditions.Select(static condition => (condition.Value, condition.Unit)).ToList();
        ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.PreferUpdatedImperialMeasurements(details, updatedFields);
        processorContext.measurementConversionService.NormalizeAttractionDetails(details);
        ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.AddUpdatedFieldIfChanged(updatedFields, "attractionDetails.heightInMeters", previousHeightInMeters, details.HeightInMeters);
        ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.AddUpdatedFieldIfChanged(updatedFields, "attractionDetails.heightInFeet", previousHeightInFeet, details.HeightInFeet);
        ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.AddUpdatedFieldIfChanged(updatedFields, "attractionDetails.lengthInMeters", previousLengthInMeters, details.LengthInMeters);
        ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.AddUpdatedFieldIfChanged(updatedFields, "attractionDetails.lengthInFeet", previousLengthInFeet, details.LengthInFeet);
        ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.AddUpdatedFieldIfChanged(updatedFields, "attractionDetails.speedInKmH", previousSpeedInKmH, details.SpeedInKmH);
        ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.AddUpdatedFieldIfChanged(updatedFields, "attractionDetails.speedInMph", previousSpeedInMph, details.SpeedInMph);
        ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.AddUpdatedFieldIfChanged(updatedFields, "attractionDetails.dropInMeters", previousDropInMeters, details.DropInMeters);
        ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.AddUpdatedFieldIfChanged(updatedFields, "attractionDetails.dropInFeet", previousDropInFeet, details.DropInFeet);
        for (int index = 0; index < previousAccessConditions.Count && index < details.AccessConditions.Count; index++)
        {
            (double? previousValue, AttractionAccessConditionUnit? previousUnit) = previousAccessConditions[index];
            AttractionAccessCondition condition = details.AccessConditions[index];
            ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.AddUpdatedFieldIfChanged(updatedFields, $"accessConditions[{index}].value", previousValue, condition.Value);
            ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.AddUpdatedFieldIfChanged(updatedFields, $"accessConditions[{index}].unit", previousUnit, condition.Unit);
        }
    }

    internal static void PreferUpdatedImperialMeasurements(AttractionDetails details, List<string> updatedFields)
    {
        if (ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.ShouldPreferUpdatedImperialMeasurement(updatedFields, "heightinfeet", new[] { "height", "heightinmeters" }, details.HeightInFeet))
        {
            details.HeightInMeters = null;
        }

        if (ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.ShouldPreferUpdatedImperialMeasurement(updatedFields, "lengthinfeet", new[] { "length", "lengthinmeters" }, details.LengthInFeet))
        {
            details.LengthInMeters = null;
        }

        if (ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.ShouldPreferUpdatedImperialMeasurement(updatedFields, "speedinmph", new[] { "speed", "speedinkmh" }, details.SpeedInMph))
        {
            details.SpeedInKmH = null;
        }

        if (ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.ShouldPreferUpdatedImperialMeasurement(updatedFields, "dropinfeet", new[] { "drop", "dropinmeters" }, details.DropInFeet))
        {
            details.DropInMeters = null;
        }
    }

    internal static bool ShouldPreferUpdatedImperialMeasurement(List<string> updatedFields, string imperialFieldName, IReadOnlyCollection<string> metricFieldNames, double? imperialValue)
    {
        return imperialValue.HasValue && ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.HasUpdatedAttractionDetailsField(updatedFields, imperialFieldName) && !metricFieldNames.Any(metricFieldName => ApplyLocalizedContentJsonCommandHandlerMeasurementsExtensions.HasUpdatedAttractionDetailsField(updatedFields, metricFieldName));
    }

    internal static bool HasUpdatedAttractionDetailsField(List<string> updatedFields, string normalizedFieldName)
    {
        foreach (string updatedField in updatedFields)
        {
            string candidate = updatedField.StartsWith("attractionDetails.", StringComparison.OrdinalIgnoreCase) ? updatedField["attractionDetails.".Length..] : updatedField;
            if (string.Equals(ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(candidate), normalizedFieldName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static void AddUpdatedFieldIfChanged<TValue>(List<string> updatedFields, string fieldName, TValue previousValue, TValue currentValue)
    {
        if (!EqualityComparer<TValue>.Default.Equals(previousValue, currentValue) && !updatedFields.Contains(fieldName))
        {
            updatedFields.Add(fieldName);
        }
    }
}
