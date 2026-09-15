using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.FactualEvents;

public sealed record ChangeTarget
{
    private ChangeTarget(
        FactualTargetType type,
        string targetId,
        string? parentParkId)
    {
        if (!Enum.IsDefined(type))
        {
            throw InvalidTarget("The factual target type is invalid.");
        }

        string normalizedTargetId = IdentifierRules.NormalizeRequired(targetId, nameof(targetId));
        string? normalizedParentParkId = string.IsNullOrWhiteSpace(parentParkId)
            ? null
            : IdentifierRules.NormalizeRequired(parentParkId, nameof(parentParkId));
        if (type == FactualTargetType.Park && normalizedParentParkId is not null)
        {
            throw InvalidTarget("A park factual target cannot declare a parent park.");
        }

        if (type == FactualTargetType.ParkItem && normalizedParentParkId is null)
        {
            throw InvalidTarget("A park item factual target must declare its parent park.");
        }

        this.Type = type;
        this.TargetId = normalizedTargetId;
        this.ParentParkId = normalizedParentParkId;
    }

    public FactualTargetType Type { get; }

    public string TargetId { get; }

    public string? ParentParkId { get; }

    public static ChangeTarget ForPark(string parkId)
    {
        return new ChangeTarget(FactualTargetType.Park, parkId, null);
    }

    public static ChangeTarget ForParkItem(string parkItemId, string parentParkId)
    {
        return new ChangeTarget(FactualTargetType.ParkItem, parkItemId, parentParkId);
    }

    public static ChangeTarget Restore(
        FactualTargetType type,
        string targetId,
        string? parentParkId)
    {
        return new ChangeTarget(type, targetId, parentParkId);
    }

    private static FactualEventValidationException InvalidTarget(string message)
    {
        return new FactualEventValidationException(
            FactualEventErrorCodes.InvalidTarget,
            message);
    }
}
