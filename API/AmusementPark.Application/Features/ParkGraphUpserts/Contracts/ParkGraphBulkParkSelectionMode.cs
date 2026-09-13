using System.Text.Json;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.Parks.Contracts;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public enum ParkGraphBulkParkSelectionMode
{
    Filtered,
    Explicit,
}
