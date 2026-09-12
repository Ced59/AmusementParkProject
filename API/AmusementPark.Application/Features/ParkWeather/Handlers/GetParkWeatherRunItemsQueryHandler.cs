using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.ParkWeather.Queries;
using AmusementPark.Application.Features.ParkWeather.Results;
using AmusementPark.Application.Features.ParkWeather.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Handlers;

public sealed class GetParkWeatherRunItemsQueryHandler : IQueryHandler<GetParkWeatherRunItemsQuery, ApplicationResult<IReadOnlyCollection<ParkWeatherRunItemResult>>>
{
    private readonly IParkWeatherRunRepository runRepository;

    public GetParkWeatherRunItemsQueryHandler(IParkWeatherRunRepository runRepository)
    {
        this.runRepository = runRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<ParkWeatherRunItemResult>>> HandleAsync(GetParkWeatherRunItemsQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.RunId))
        {
            return ApplicationResult<IReadOnlyCollection<ParkWeatherRunItemResult>>.Failure(ParkWeatherApplicationErrors.RunNotFound());
        }

        ParkWeatherRun? run = await this.runRepository.GetByIdAsync(query.RunId.Trim(), cancellationToken);
        if (run is null)
        {
            return ApplicationResult<IReadOnlyCollection<ParkWeatherRunItemResult>>.Failure(ParkWeatherApplicationErrors.RunNotFound());
        }

        ParkWeatherRunItemStatus? status = ParseStatus(query.Status);
        IReadOnlyCollection<ParkWeatherRunItem> items = await this.runRepository.GetRunItemsAsync(run.Id ?? string.Empty, status, cancellationToken);
        IReadOnlyCollection<ParkWeatherRunItemResult> results = items
            .OrderBy(static item => item.ParkName)
            .ThenBy(static item => item.ParkId)
            .Select(static item => item.ToResult())
            .ToList();

        return ApplicationResult<IReadOnlyCollection<ParkWeatherRunItemResult>>.Success(results);
    }

    private static ParkWeatherRunItemStatus? ParseStatus(string? status)
    {
        return Enum.TryParse(status, true, out ParkWeatherRunItemStatus parsed)
            ? parsed
            : null;
    }
}
