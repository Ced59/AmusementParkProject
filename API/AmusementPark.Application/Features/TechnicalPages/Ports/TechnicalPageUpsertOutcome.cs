using AmusementPark.Core.Domain.TechnicalPages;

namespace AmusementPark.Application.Features.TechnicalPages.Ports;

public sealed record TechnicalPageUpsertOutcome(TechnicalPage Page, bool Created);
