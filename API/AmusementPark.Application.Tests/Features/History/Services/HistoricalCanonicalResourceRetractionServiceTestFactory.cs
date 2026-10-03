using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using Moq;

namespace AmusementPark.Application.Tests.Features.History.Services;

internal static class HistoricalCanonicalResourceRetractionServiceTestFactory
{
    public static HistoricalCanonicalResourceRetractionService Create(
        IHistoricalFactRepository factRepository,
        IHistoricalSourceRepository? sourceRepository = null)
    {
        IHistoricalSourceRepository resolvedSourceRepository = sourceRepository
            ?? Mock.Of<IHistoricalSourceRepository>();
        return new HistoricalCanonicalResourceRetractionService(
            new HistoricalNarrativeCanonicalFactRetractionService(factRepository),
            new HistoricalCanonicalSourceRetractionService(resolvedSourceRepository));
    }
}
