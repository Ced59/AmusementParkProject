using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.LocalizedContent.Handlers;

public sealed partial class ApplyLocalizedContentJsonCommandHandler
{
    private void NormalizeAttractionDetailsAfterPatch(AttractionDetails details, List<string> updatedFields)
    {
        double? previousHeightInMeters = details.HeightInMeters;
        double? previousHeightInFeet = details.HeightInFeet;
        double? previousLengthInMeters = details.LengthInMeters;
        double? previousLengthInFeet = details.LengthInFeet;
        double? previousSpeedInKmH = details.SpeedInKmH;
        double? previousSpeedInMph = details.SpeedInMph;
        double? previousDropInMeters = details.DropInMeters;
        double? previousDropInFeet = details.DropInFeet;
        List<(double? Value, AttractionAccessConditionUnit? Unit)> previousAccessConditions = details.AccessConditions
            .Select(static condition => (condition.Value, condition.Unit))
            .ToList();

        PreferUpdatedImperialMeasurements(details, updatedFields);
        this.measurementConversionService.NormalizeAttractionDetails(details);

        AddUpdatedFieldIfChanged(updatedFields, "attractionDetails.heightInMeters", previousHeightInMeters, details.HeightInMeters);
        AddUpdatedFieldIfChanged(updatedFields, "attractionDetails.heightInFeet", previousHeightInFeet, details.HeightInFeet);
        AddUpdatedFieldIfChanged(updatedFields, "attractionDetails.lengthInMeters", previousLengthInMeters, details.LengthInMeters);
        AddUpdatedFieldIfChanged(updatedFields, "attractionDetails.lengthInFeet", previousLengthInFeet, details.LengthInFeet);
        AddUpdatedFieldIfChanged(updatedFields, "attractionDetails.speedInKmH", previousSpeedInKmH, details.SpeedInKmH);
        AddUpdatedFieldIfChanged(updatedFields, "attractionDetails.speedInMph", previousSpeedInMph, details.SpeedInMph);
        AddUpdatedFieldIfChanged(updatedFields, "attractionDetails.dropInMeters", previousDropInMeters, details.DropInMeters);
        AddUpdatedFieldIfChanged(updatedFields, "attractionDetails.dropInFeet", previousDropInFeet, details.DropInFeet);

        for (int index = 0; index < previousAccessConditions.Count && index < details.AccessConditions.Count; index++)
        {
            (double? previousValue, AttractionAccessConditionUnit? previousUnit) = previousAccessConditions[index];
            AttractionAccessCondition condition = details.AccessConditions[index];
            AddUpdatedFieldIfChanged(updatedFields, $"accessConditions[{index}].value", previousValue, condition.Value);
            AddUpdatedFieldIfChanged(updatedFields, $"accessConditions[{index}].unit", previousUnit, condition.Unit);
        }
    }

    private static void PreferUpdatedImperialMeasurements(AttractionDetails details, List<string> updatedFields)
    {
        if (ShouldPreferUpdatedImperialMeasurement(updatedFields, "heightinfeet", new[] { "height", "heightinmeters" }, details.HeightInFeet))
        {
            details.HeightInMeters = null;
        }

        if (ShouldPreferUpdatedImperialMeasurement(updatedFields, "lengthinfeet", new[] { "length", "lengthinmeters" }, details.LengthInFeet))
        {
            details.LengthInMeters = null;
        }

        if (ShouldPreferUpdatedImperialMeasurement(updatedFields, "speedinmph", new[] { "speed", "speedinkmh" }, details.SpeedInMph))
        {
            details.SpeedInKmH = null;
        }

        if (ShouldPreferUpdatedImperialMeasurement(updatedFields, "dropinfeet", new[] { "drop", "dropinmeters" }, details.DropInFeet))
        {
            details.DropInMeters = null;
        }
    }

    private static bool ShouldPreferUpdatedImperialMeasurement(List<string> updatedFields, string imperialFieldName, IReadOnlyCollection<string> metricFieldNames, double? imperialValue)
    {
        return imperialValue.HasValue
            && HasUpdatedAttractionDetailsField(updatedFields, imperialFieldName)
            && !metricFieldNames.Any(metricFieldName => HasUpdatedAttractionDetailsField(updatedFields, metricFieldName));
    }

    private static bool HasUpdatedAttractionDetailsField(List<string> updatedFields, string normalizedFieldName)
    {
        foreach (string updatedField in updatedFields)
        {
            string candidate = updatedField.StartsWith("attractionDetails.", StringComparison.OrdinalIgnoreCase)
                ? updatedField["attractionDetails.".Length..]
                : updatedField;
            if (string.Equals(NormalizeField(candidate), normalizedFieldName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void AddUpdatedFieldIfChanged<TValue>(List<string> updatedFields, string fieldName, TValue previousValue, TValue currentValue)
    {
        if (!EqualityComparer<TValue>.Default.Equals(previousValue, currentValue) && !updatedFields.Contains(fieldName))
        {
            updatedFields.Add(fieldName);
        }
    }
}
