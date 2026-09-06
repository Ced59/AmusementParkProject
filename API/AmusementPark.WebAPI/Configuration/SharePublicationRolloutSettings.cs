namespace AmusementPark.WebAPI.Configuration;

/// <summary>
/// Controls availability of share-publication endpoints during a rolling deployment.
/// </summary>
public sealed class SharePublicationRolloutSettings
{
    public const string SectionName = "Sharing:SharePublicationPreview";

    public bool Enabled { get; init; } = true;
}
