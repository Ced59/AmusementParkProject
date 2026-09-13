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
/// Repository Mongo des exploitants.
/// </summary>
public sealed class ParkOperatorRepository : MongoCrudRepositoryBase<ParkOperator, ParkOperatorDocument>, IParkOperatorRepository
{
    public ParkOperatorRepository(IMongoDatabase database, MongoDbSettings settings)
        : base(database.GetCollection<ParkOperatorDocument>(settings.ParkOperatorsCollectionName))
    {
    }

    public Task<IReadOnlyCollection<ParkOperator>> GetAllAsync(CancellationToken cancellationToken)
    {
        return base.GetAllAsync(document => document.ToDomain(), cancellationToken);
    }

    public Task<ParkOperator?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return base.GetByIdAsync(id, document => document.ToDomain(), cancellationToken);
    }

    public Task<IReadOnlyCollection<ParkOperator>> GetByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken)
    {
        return base.GetByIdsAsync(ids, document => document.ToDomain(), cancellationToken);
    }

    public Task<ParkOperator> CreateAsync(ParkOperator entity, CancellationToken cancellationToken)
    {
        return base.CreateAsync(entity, value => value.ToDocument(), document => document.ToDomain(), cancellationToken);
    }

    public Task<ParkOperator?> UpdateAsync(string id, ParkOperator entity, CancellationToken cancellationToken)
    {
        return base.UpdateAsync(id, entity, value => value.ToDocument(), document => document.ToDomain(), cancellationToken);
    }

    public Task<int> UpdateBulkAdminReviewStatusAsync(IReadOnlyCollection<string> ids, AdminReviewStatus adminReviewStatus, CancellationToken cancellationToken)
    {
        return base.UpdateBulkAdminReviewStatusAsync(ids, adminReviewStatus, cancellationToken);
    }
}
