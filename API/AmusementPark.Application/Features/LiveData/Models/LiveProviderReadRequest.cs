using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Application.Features.LiveData.Models;

public sealed class LiveProviderReadRequest
{
    public const int MaximumEntityTagLength = 500;

    public LiveProviderReadRequest(string externalEntityId, string? entityTag = null)
    {
        string normalizedExternalEntityId = IdentifierRules.NormalizeRequired(
            externalEntityId,
            nameof(externalEntityId));

        string? normalizedEntityTag = string.IsNullOrWhiteSpace(entityTag)
            ? null
            : entityTag.Trim();
        if (normalizedEntityTag is not null
            && (normalizedEntityTag.Length > MaximumEntityTagLength
                || normalizedEntityTag.Any(char.IsControl)))
        {
            throw new ArgumentException(
                "A provider entity tag cannot exceed 500 characters.",
                nameof(entityTag));
        }

        this.ExternalEntityId = normalizedExternalEntityId;
        this.EntityTag = normalizedEntityTag;
    }

    public string ExternalEntityId { get; }

    public string? EntityTag { get; }
}
