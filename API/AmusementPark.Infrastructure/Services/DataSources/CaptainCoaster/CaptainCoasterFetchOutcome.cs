using AmusementPark.Application.Features.DataSources.Contracts;
using AmusementPark.Application.Features.DataSources.Results;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Services.DataSources.Acquisition;
using AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using System.Globalization;
using System.Threading.Channels;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AmusementPark.Application.Features.Search;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster;

internal sealed record CaptainCoasterFetchOutcome(
    CaptainCoasterDiscoveredUrl? DiscoveredUrl,
    CaptainCoasterCoasterSnapshotDocument? Document,
    string? ErrorMessage)
{
    public static CaptainCoasterFetchOutcome Success(CaptainCoasterCoasterSnapshotDocument document)
    {
        return new CaptainCoasterFetchOutcome(null, document, null);
    }

    public static CaptainCoasterFetchOutcome Failure(CaptainCoasterDiscoveredUrl discoveredUrl, string errorMessage)
    {
        return new CaptainCoasterFetchOutcome(discoveredUrl, null, errorMessage);
    }
}
