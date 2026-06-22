using AmusementPark.Core.Domain.TechnicalPages;

namespace AmusementPark.Application.Features.TechnicalPages.Ports;

public interface ITechnicalPageRepository
{
    Task<IReadOnlyCollection<TechnicalPage>> GetAllAsync(bool includeHidden, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TechnicalPage>> GetPublicLinkIndexAsync(CancellationToken cancellationToken);

    Task<TechnicalPage?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<TechnicalPage?> GetBySlugAsync(string slug, bool includeHidden, CancellationToken cancellationToken);

    Task<TechnicalPage> CreateAsync(TechnicalPage page, CancellationToken cancellationToken);

    Task<TechnicalPage?> UpdateAsync(string id, TechnicalPage page, CancellationToken cancellationToken);

    Task<TechnicalPageUpsertOutcome> UpsertBySlugAsync(TechnicalPage page, CancellationToken cancellationToken);
}

public sealed record TechnicalPageUpsertOutcome(TechnicalPage Page, bool Created);
