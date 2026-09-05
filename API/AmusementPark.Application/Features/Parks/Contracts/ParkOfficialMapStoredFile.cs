using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Contracts;

public sealed class ParkOfficialMapStoredFile
{
    public required string StorageKey { get; init; }

    public required string OriginalFileName { get; init; }

    public required string ContentType { get; init; }

    public long SizeInBytes { get; init; }

    public ParkOfficialMapFormat SuggestedFormat { get; init; }
}
