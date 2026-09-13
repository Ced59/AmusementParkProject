using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Core.Domain.Countries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Countries;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Repository Mongo des fondateurs.
/// </summary>
public sealed class ParkFounderRepository : MongoCrudRepositoryBase<ParkFounder, ParkFounderDocument>, IParkFounderRepository
{
    public ParkFounderRepository(IMongoDatabase database, MongoDbSettings settings)
        : base(database.GetCollection<ParkFounderDocument>(settings.ParkFoundersCollectionName))
    {
    }

    public Task<IReadOnlyCollection<ParkFounder>> GetAllAsync(CancellationToken cancellationToken)
    {
        return base.GetAllAsync(document => document.ToDomain(), cancellationToken);
    }

    public Task<ParkFounder?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return base.GetByIdAsync(id, document => document.ToDomain(), cancellationToken);
    }

    public Task<IReadOnlyCollection<ParkFounder>> GetByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken)
    {
        return base.GetByIdsAsync(ids, document => document.ToDomain(), cancellationToken);
    }

    public Task<ParkFounder> CreateAsync(ParkFounder entity, CancellationToken cancellationToken)
    {
        return base.CreateAsync(entity, value => value.ToDocument(), document => document.ToDomain(), cancellationToken);
    }

    public Task<ParkFounder?> UpdateAsync(string id, ParkFounder entity, CancellationToken cancellationToken)
    {
        return base.UpdateAsync(id, entity, value => value.ToDocument(), document => document.ToDomain(), cancellationToken);
    }
}
