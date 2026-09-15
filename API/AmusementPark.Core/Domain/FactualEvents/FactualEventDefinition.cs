using System.Collections.Frozen;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.FactualEvents;

public sealed class FactualEventDefinition
{
    internal FactualEventDefinition(
        FactualEventType type,
        string code,
        int schemaVersion,
        IReadOnlyCollection<FactualTargetType> supportedTargetTypes)
    {
        if (!Enum.IsDefined(type))
        {
            throw InvalidDefinition("The factual event type is invalid.");
        }

        string normalizedCode = IdentifierRules.NormalizeRequired(code, nameof(code));
        if (schemaVersion < 1)
        {
            throw InvalidDefinition("The factual event schema version must be positive.");
        }

        ArgumentNullException.ThrowIfNull(supportedTargetTypes);
        if (supportedTargetTypes.Count == 0
            || supportedTargetTypes.Any(static targetType => !Enum.IsDefined(targetType)))
        {
            throw InvalidDefinition("A factual event must support at least one valid target type.");
        }

        this.Type = type;
        this.Code = normalizedCode;
        this.SchemaVersion = schemaVersion;
        this.SupportedTargetTypes = supportedTargetTypes.ToFrozenSet();
    }

    public FactualEventType Type { get; }

    public string Code { get; }

    public int SchemaVersion { get; }

    public IReadOnlySet<FactualTargetType> SupportedTargetTypes { get; }

    public bool Supports(FactualTargetType targetType)
    {
        return Enum.IsDefined(targetType) && this.SupportedTargetTypes.Contains(targetType);
    }

    private static FactualEventValidationException InvalidDefinition(string message)
    {
        return new FactualEventValidationException(
            FactualEventErrorCodes.InvalidDefinition,
            message);
    }
}
