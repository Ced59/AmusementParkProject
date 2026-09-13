namespace AmusementPark.Infrastructure.Services.DataSources.Acquisition;

/// <summary>
/// Télécharge du contenu texte sans persister les payloads sur disque.
/// </summary>
internal interface IDataAcquisitionHttpFetcher
{
    Task<string> GetStringAsync(string url, string acceptLanguage, DataAcquisitionRequestOptions options, CancellationToken cancellationToken);
}
