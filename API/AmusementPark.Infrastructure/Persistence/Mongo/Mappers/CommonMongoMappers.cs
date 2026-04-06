using AmusementPark.Application.Common.Contracts;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

/// <summary>
/// Helpers de mapping communs entre domaine, contrats applicatifs et documents Mongo.
/// </summary>
internal static class CommonMongoMappers
{
    public static List<LocalizedText> ToDomain(IReadOnlyCollection<LocalizedTextDocument>? documents)
    {
        List<LocalizedText> result = new List<LocalizedText>();

        if (documents is null)
        {
            return result;
        }

        foreach (LocalizedTextDocument document in documents)
        {
            result.Add(new LocalizedText(document.LanguageCode, document.Value));
        }

        return result;
    }

    public static List<LocalizedTextDocument> ToDocuments(IReadOnlyCollection<LocalizedText>? values)
    {
        List<LocalizedTextDocument> result = new List<LocalizedTextDocument>();

        if (values is null)
        {
            return result;
        }

        foreach (LocalizedText value in values)
        {
            result.Add(new LocalizedTextDocument
            {
                LanguageCode = value.LanguageCode,
                Value = value.Value,
            });
        }

        return result;
    }

    public static List<LocalizedTextDocument> ToDocuments(IReadOnlyCollection<LocalizedTextValue>? values)
    {
        List<LocalizedTextDocument> result = new List<LocalizedTextDocument>();

        if (values is null)
        {
            return result;
        }

        foreach (LocalizedTextValue value in values)
        {
            result.Add(new LocalizedTextDocument
            {
                LanguageCode = value.LanguageCode,
                Value = value.Value,
            });
        }

        return result;
    }

    public static GeoPointDocument? ToDocument(GeoPoint? value)
    {
        if (value is null)
        {
            return null;
        }

        return new GeoPointDocument
        {
            Latitude = value.Latitude,
            Longitude = value.Longitude,
        };
    }

    public static GeoPoint? ToDomain(GeoPointDocument? value)
    {
        if (value is null || !value.Latitude.HasValue || !value.Longitude.HasValue)
        {
            return null;
        }

        return new GeoPoint(value.Latitude.Value, value.Longitude.Value);
    }

    public static void ApplyPosition(AmusementPark.Core.Geo.GeolocatedEntityBase entity, double? latitude, double? longitude)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (latitude.HasValue && longitude.HasValue)
        {
            entity.SetPosition(latitude.Value, longitude.Value);
        }
    }

    public static void ApplyPosition(AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common.MongoGeolocatedDocumentBase document, GeoPoint? position)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (position is null)
        {
            document.Latitude = null;
            document.Longitude = null;
            document.Location = null;
            return;
        }

        document.Latitude = position.Latitude;
        document.Longitude = position.Longitude;
        document.RefreshLocation();
    }
}
