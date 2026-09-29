using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveOperationalControlScope : IEquatable<LiveOperationalControlScope>
{
    public LiveOperationalControlScope(
        LiveOperationalScopeType type,
        LiveDataSourceId sourceId,
        string? externalEntityId,
        string? internalParkId,
        LiveTargetType? targetType,
        string? internalTargetId)
    {
        if (!Enum.IsDefined(type)
            || targetType.HasValue && !Enum.IsDefined(targetType.Value))
        {
            throw Invalid("The live operational control scope contains an invalid type.");
        }

        _ = sourceId.Value;
        string? normalizedExternalEntityId = NormalizeOptional(externalEntityId, nameof(externalEntityId));
        string? normalizedParkId = NormalizeOptional(internalParkId, nameof(internalParkId));
        string? normalizedTargetId = NormalizeOptional(internalTargetId, nameof(internalTargetId));
        bool valid = type switch
        {
            LiveOperationalScopeType.Source => normalizedExternalEntityId is null
                && normalizedParkId is null
                && targetType is null
                && normalizedTargetId is null,
            LiveOperationalScopeType.Park => normalizedExternalEntityId is not null
                && normalizedParkId is not null
                && targetType is null
                && normalizedTargetId is null,
            LiveOperationalScopeType.Target => normalizedExternalEntityId is not null
                && normalizedParkId is not null
                && targetType.HasValue
                && targetType.Value != LiveTargetType.Park
                && normalizedTargetId is not null,
            _ => false,
        };
        if (!valid)
        {
            throw Invalid("The live operational control scope is inconsistent.");
        }

        this.Type = type;
        this.SourceId = sourceId;
        this.ExternalEntityId = normalizedExternalEntityId;
        this.InternalParkId = normalizedParkId;
        this.TargetType = targetType;
        this.InternalTargetId = normalizedTargetId;
    }

    public LiveOperationalScopeType Type { get; }

    public LiveDataSourceId SourceId { get; }

    public string? ExternalEntityId { get; }

    public string? InternalParkId { get; }

    public LiveTargetType? TargetType { get; }

    public string? InternalTargetId { get; }

    public bool AppliesTo(
        string externalEntityId,
        string? internalParkId,
        LiveTargetType? targetType,
        string? internalTargetId)
    {
        if (this.Type == LiveOperationalScopeType.Source)
        {
            return true;
        }

        if (!string.Equals(this.ExternalEntityId, externalEntityId, StringComparison.Ordinal))
        {
            return false;
        }

        if (this.Type == LiveOperationalScopeType.Park)
        {
            return internalParkId is null
                || string.Equals(this.InternalParkId, internalParkId, StringComparison.Ordinal);
        }

        return targetType.HasValue
            && internalParkId is not null
            && internalTargetId is not null
            && string.Equals(this.InternalParkId, internalParkId, StringComparison.Ordinal)
            && this.TargetType == targetType
            && string.Equals(this.InternalTargetId, internalTargetId, StringComparison.Ordinal);
    }

    public bool Equals(LiveOperationalControlScope? other)
    {
        return other is not null
            && this.Type == other.Type
            && this.SourceId == other.SourceId
            && string.Equals(this.ExternalEntityId, other.ExternalEntityId, StringComparison.Ordinal)
            && string.Equals(this.InternalParkId, other.InternalParkId, StringComparison.Ordinal)
            && this.TargetType == other.TargetType
            && string.Equals(this.InternalTargetId, other.InternalTargetId, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is LiveOperationalControlScope other && this.Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            this.Type,
            this.SourceId,
            this.ExternalEntityId,
            this.InternalParkId,
            this.TargetType,
            this.InternalTargetId);
    }

    private static string? NormalizeOptional(string? value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : IdentifierRules.NormalizeRequired(value, parameterName);
    }

    private static LiveDataValidationException Invalid(string message)
    {
        return new LiveDataValidationException(
            LiveDataErrorCodes.InvalidOperationalControl,
            message);
    }
}
