namespace AmusementPark.Application.Features.Parks.Contracts;

public sealed class ParkOfficialMapBinary
{
    public required Func<Stream, long, long?, CancellationToken, Task> CopyToAsync { get; init; }

    public required string ContentType { get; init; }

    public required string FileName { get; init; }

    public long SizeInBytes { get; init; }

    public bool DisplayInline { get; init; }
}
