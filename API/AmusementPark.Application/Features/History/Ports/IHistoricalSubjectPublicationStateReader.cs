using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Ports;

public interface IHistoricalSubjectPublicationStateReader
{
    Task<bool> IsPublicAsync(HistoricalSubject subject, CancellationToken cancellationToken);
}
