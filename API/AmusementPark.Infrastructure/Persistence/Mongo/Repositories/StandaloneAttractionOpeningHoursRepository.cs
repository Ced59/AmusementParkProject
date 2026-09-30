using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class StandaloneAttractionOpeningHoursRepository : IStandaloneAttractionOpeningHoursRepository
{
    private readonly ParkOpeningHoursRepository repository;

    public StandaloneAttractionOpeningHoursRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.repository = new ParkOpeningHoursRepository(
            database,
            settings.StandaloneAttractionOpeningHoursCollectionName);
    }

    public Task<ParkOpeningHoursSchedule?> GetByStandaloneAttractionIdAsync(
        string standaloneAttractionId,
        CancellationToken cancellationToken)
    {
        return this.repository.GetByParkIdAsync(standaloneAttractionId, cancellationToken);
    }

    public Task<ParkOpeningHoursSchedule> UpsertAsync(
        ParkOpeningHoursSchedule schedule,
        CancellationToken cancellationToken)
    {
        return this.repository.UpsertAsync(schedule, cancellationToken);
    }
}
