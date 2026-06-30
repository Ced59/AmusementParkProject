using System.Text;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Results;

public sealed class ParkGraphJsonExportResult
{
    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = "application/json";

    public byte[] Content { get; init; } = Array.Empty<byte>();

    public string Json => Encoding.UTF8.GetString(this.Content);
}
