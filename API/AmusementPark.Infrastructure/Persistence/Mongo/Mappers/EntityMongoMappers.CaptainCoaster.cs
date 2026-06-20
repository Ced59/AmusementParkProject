using AmusementPark.Application.Features.CaptainCoaster.Results;
using AmusementPark.Core.Domain.Countries;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Countries;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Search;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

/// <summary>
/// Mappers centralisés domaine/resultats applicatifs &lt;-&gt; documents Mongo.
/// </summary>
internal static partial class EntityMongoMappers
{
    public static CaptainCoasterSettingsResult ToResult(this CaptainCoasterSettingsDocument document)
    {
        return new CaptainCoasterSettingsResult
        {
            IsEnabled = document.IsEnabled,
            DataDirectoryPath = document.DataDirectoryPath,
            HtmlDirectoryPath = document.HtmlDirectoryPath,
            UseOfflineMode = document.UseOfflineMode,
        };
    }

    public static CaptainCoasterSettingsDocument ToDocument(this CaptainCoasterSettingsResult result)
    {
        return new CaptainCoasterSettingsDocument
        {
            IsEnabled = result.IsEnabled,
            DataDirectoryPath = result.DataDirectoryPath,
            HtmlDirectoryPath = result.HtmlDirectoryPath,
            UseOfflineMode = result.UseOfflineMode,
        };
    }

    public static CaptainCoasterSessionResult ToResult(this CaptainCoasterSyncSessionDocument document)
    {
        return new CaptainCoasterSessionResult
        {
            SessionId = document.Id,
            Status = document.Status,
            ProgressPercentage = document.ProgressPercentage,
            Message = document.Message,
        };
    }

    public static AmusementPark.Application.Features.Search.Results.SearchHitResult ToSearchHit(this SearchItemDocument document, string? languageCode = null)
    {
        string? localizedDescription = SearchLocalizedTextResolver.Resolve(document.LocalizedDescriptions, languageCode);

        return new AmusementPark.Application.Features.Search.Results.SearchHitResult
        {
            Id = string.IsNullOrWhiteSpace(document.OriginalId) ? document.Id : document.OriginalId,
            ResourceType = string.IsNullOrWhiteSpace(document.ResourceType) ? document.Category : document.ResourceType,
            Title = document.Title,
            Subtitle = document.Subtitle,
            Category = document.Category,
            Description = localizedDescription ?? document.Description,
            City = document.City,
            CountryCode = document.CountryCode,
            LogoImageId = document.LogoImageId,
            AttractionCount = document.AttractionCount,
            ParentParkId = document.ParentParkId,
            ParentParkName = document.ParentParkName,
            Score = document.CompositeScore,
        };
    }
}
