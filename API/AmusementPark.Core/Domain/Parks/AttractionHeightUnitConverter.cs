namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Normalise les seuils de taille utilisés par les règles d'accès.
/// </summary>
public static class AttractionHeightUnitConverter
{
    private const double CentimetersPerInch = 2.54d;

    public static bool TryConvertToCentimeters(
        AttractionAccessCondition condition,
        out double centimeters)
    {
        ArgumentNullException.ThrowIfNull(condition);

        centimeters = 0;
        if (!condition.Value.HasValue
            || !double.IsFinite(condition.Value.Value)
            || condition.Value.Value <= 0)
        {
            return false;
        }

        if (condition.Unit == AttractionAccessConditionUnit.Centimeter)
        {
            centimeters = condition.Value.Value;
            return true;
        }

        if (condition.Unit == AttractionAccessConditionUnit.Inch)
        {
            centimeters = condition.Value.Value * CentimetersPerInch;
            return true;
        }

        return false;
    }
}
