using AmusementPark.Application.Features.Ratings.Ports;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed record PublicParkItemEvidenceReadResult(
    IReadOnlyCollection<PublicParkItemEvidenceFact> Facts,
    IReadOnlyCollection<string> IncompleteParkIds);
