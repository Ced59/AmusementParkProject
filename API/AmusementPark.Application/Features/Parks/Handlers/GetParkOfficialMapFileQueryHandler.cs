using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Services;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

public sealed class GetParkOfficialMapFileQueryHandler
    : IQueryHandler<GetParkOfficialMapFileQuery, ApplicationResult<ParkOfficialMapBinary>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkOfficialMapBinaryStorage storage;

    public GetParkOfficialMapFileQueryHandler(
        IParkRepository parkRepository,
        IParkOfficialMapBinaryStorage storage)
    {
        this.parkRepository = parkRepository;
        this.storage = storage;
    }

    public async Task<ApplicationResult<ParkOfficialMapBinary>> HandleAsync(
        GetParkOfficialMapFileQuery query,
        CancellationToken cancellationToken = default)
    {
        Park? park = await this.parkRepository.GetByIdAsync(query.ParkId.Trim(), query.IncludeHidden, cancellationToken);
        ParkOfficialMap? officialMap = park?.OfficialMaps.FirstOrDefault(map =>
            string.Equals(map.Id, query.OfficialMapId.Trim(), StringComparison.OrdinalIgnoreCase));
        if (park is null
            || officialMap is null
            || (!query.IncludeHidden && !officialMap.IsPubliclyDisplayable())
            || string.IsNullOrWhiteSpace(officialMap.StorageKey)
            || !ParkOfficialMapStorageKeys.BelongsTo(officialMap.StorageKey, park.Id, officialMap.Id))
        {
            return ApplicationResult<ParkOfficialMapBinary>.Failure(ParkApplicationErrors.OfficialMapFileNotFound());
        }

        bool exists = await this.storage.ExistsAsync(officialMap.StorageKey, cancellationToken);
        if (!exists)
        {
            return ApplicationResult<ParkOfficialMapBinary>.Failure(ParkApplicationErrors.OfficialMapFileNotFound());
        }

        string contentType = officialMap.ContentType ?? "application/octet-stream";
        return ApplicationResult<ParkOfficialMapBinary>.Success(new ParkOfficialMapBinary
        {
            CopyToAsync = (destination, offset, length, writeCancellationToken) => this.storage.CopyToAsync(
                officialMap.StorageKey,
                destination,
                offset,
                length,
                writeCancellationToken),
            ContentType = contentType,
            FileName = string.IsNullOrWhiteSpace(officialMap.OriginalFileName)
                ? $"official-map-{officialMap.Year}"
                : officialMap.OriginalFileName,
            SizeInBytes = officialMap.SizeInBytes ?? 0,
            DisplayInline = officialMap.Format is ParkOfficialMapFormat.Image or ParkOfficialMapFormat.Pdf,
        });
    }
}
