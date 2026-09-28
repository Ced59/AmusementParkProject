namespace AmusementPark.Application.Features.LiveData.Models;

public sealed class LiveProviderReadRequest
{
    public LiveProviderReadRequest(string externalEntityId, string? entityTag = null)
    {
        string normalizedExternalEntityId = externalEntityId?.Trim() ?? string.Empty;
        if (normalizedExternalEntityId.Length is 0 or > 200
            || normalizedExternalEntityId.Any(char.IsControl))
        {
            throw new ArgumentException(
                "A provider entity identifier is required and cannot exceed 200 characters.",
                nameof(externalEntityId));
        }

        string? normalizedEntityTag = string.IsNullOrWhiteSpace(entityTag)
            ? null
            : entityTag.Trim();
        if (normalizedEntityTag is not null
            && (normalizedEntityTag.Length > 500 || normalizedEntityTag.Any(char.IsControl)))
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
