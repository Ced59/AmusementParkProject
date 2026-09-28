using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Application.Features.LiveData.Models;

public sealed class LiveProviderReadRequest
{
    public const int MaximumEntityTagLength = LiveProviderEntityTag.MaximumLength;

    public LiveProviderReadRequest(string externalEntityId, string? entityTag = null)
    {
        string normalizedExternalEntityId = IdentifierRules.NormalizeRequired(
            externalEntityId,
            nameof(externalEntityId));
        if (normalizedExternalEntityId is "." or "..")
        {
            throw new ArgumentException(
                "A provider entity identifier cannot be a relative path segment.",
                nameof(externalEntityId));
        }

        if (!LiveProviderEntityTag.TryNormalize(entityTag, out string? normalizedEntityTag))
        {
            throw new ArgumentException(
                "A provider entity tag must be a concrete strong or weak HTTP entity tag.",
                nameof(entityTag));
        }

        this.ExternalEntityId = normalizedExternalEntityId;
        this.EntityTag = normalizedEntityTag;
    }

    public string ExternalEntityId { get; }

    public string? EntityTag { get; }
}
