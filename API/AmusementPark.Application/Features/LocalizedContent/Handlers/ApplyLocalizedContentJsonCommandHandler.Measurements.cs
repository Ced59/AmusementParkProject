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

    private static void AddUpdatedFieldIfChanged<TValue>(List<string> updatedFields, string fieldName, TValue previousValue, TValue currentValue)
    {
        if (!EqualityComparer<TValue>.Default.Equals(previousValue, currentValue) && !updatedFields.Contains(fieldName))
        {
            updatedFields.Add(fieldName);
        }
    }
}
