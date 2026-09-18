namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TransferTripOwnershipRequestDto
{
    public string PreviousOwnerRole { get; set; } = string.Empty;

    public long ExpectedVersion { get; set; }
}
