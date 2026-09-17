namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class VersionedMutationRequestDto
{
    public long ExpectedVersion { get; init; }
}
