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

internal sealed class CaptainCoasterApplyExecutionContext
{
    public CaptainCoasterApplyExecutionContext(
        List<ParkDocument> localParks,
        List<ParkItemDocument> localCoasters,
        List<AttractionManufacturerDocument> manufacturers,
        List<CaptainCoasterParkSnapshotDocument> parkSnapshots,
        List<CaptainCoasterCoasterSnapshotDocument> coasterSnapshots)
    {
        this.LocalParks = localParks;
        this.LocalCoasters = localCoasters;
        this.LocalParksById = localParks.ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);
        this.LocalCoastersById = localCoasters.ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);
        this.ManufacturersByNormalizedName = manufacturers
            .GroupBy(item => CaptainCoasterDataSourceProvider.Normalize(item.Name), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        this.ParkSnapshotsById = parkSnapshots.ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);
        this.ParkSnapshotsByCaptainCoasterId = parkSnapshots
            .Where(item => !string.IsNullOrWhiteSpace(item.CaptainCoasterId))
            .GroupBy(item => item.CaptainCoasterId.Trim(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        this.CoasterSnapshotsById = coasterSnapshots.ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);
        this.CoasterSnapshotsByCaptainCoasterId = coasterSnapshots
            .Where(item => !string.IsNullOrWhiteSpace(item.CaptainCoasterId))
            .GroupBy(item => item.CaptainCoasterId.Trim(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        this.ParkIdsByNormalizedName = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        this.ParkIdsByNormalizedNameAndCountry = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        this.CoasterIdsByNormalizedName = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        this.CoasterIdsByNormalizedNameAndParkId = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (ParkDocument parkDocument in localParks)
        {
            CaptainCoasterDataSourceProvider.AddParkLookup(this, parkDocument);
        }

        foreach (ParkItemDocument parkItemDocument in localCoasters)
        {
            CaptainCoasterDataSourceProvider.AddCoasterLookup(this, parkItemDocument);
        }
    }

    public List<ParkDocument> LocalParks { get; }

    public List<ParkItemDocument> LocalCoasters { get; }

    public Dictionary<string, ParkDocument> LocalParksById { get; }

    public Dictionary<string, ParkItemDocument> LocalCoastersById { get; }

    public Dictionary<string, AttractionManufacturerDocument> ManufacturersByNormalizedName { get; }

    public Dictionary<string, CaptainCoasterParkSnapshotDocument> ParkSnapshotsById { get; }

    public Dictionary<string, List<CaptainCoasterParkSnapshotDocument>> ParkSnapshotsByCaptainCoasterId { get; }

    public Dictionary<string, CaptainCoasterCoasterSnapshotDocument> CoasterSnapshotsById { get; }

    public Dictionary<string, List<CaptainCoasterCoasterSnapshotDocument>> CoasterSnapshotsByCaptainCoasterId { get; }

    public Dictionary<string, List<string>> ParkIdsByNormalizedName { get; }

    public Dictionary<string, List<string>> ParkIdsByNormalizedNameAndCountry { get; }

    public Dictionary<string, List<string>> CoasterIdsByNormalizedName { get; }

    public Dictionary<string, List<string>> CoasterIdsByNormalizedNameAndParkId { get; }

    public List<WriteModel<ParkDocument>> PendingParkWrites { get; } = new List<WriteModel<ParkDocument>>();

    public List<WriteModel<ParkItemDocument>> PendingParkItemWrites { get; } = new List<WriteModel<ParkItemDocument>>();

    public List<WriteModel<AttractionManufacturerDocument>> PendingManufacturerWrites { get; } = new List<WriteModel<AttractionManufacturerDocument>>();

    public List<WriteModel<CaptainCoasterComparisonResultDocument>> PendingComparisonWrites { get; } = new List<WriteModel<CaptainCoasterComparisonResultDocument>>();

    public HashSet<string> AffectedParkIds { get; } = new HashSet<string>(StringComparer.Ordinal);

    public HashSet<string> AffectedParkItemIds { get; } = new HashSet<string>(StringComparer.Ordinal);
}
