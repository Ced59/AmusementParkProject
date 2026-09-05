using AmusementPark.Application.Common.Contracts;

namespace AmusementPark.Application.Features.Parks.Contracts;

public sealed record ParkOfficialMapFileUploadRequest(
    string ParkId,
    string OfficialMapId,
    FilePayload File);
