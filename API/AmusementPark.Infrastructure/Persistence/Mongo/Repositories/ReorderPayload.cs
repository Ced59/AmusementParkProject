using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed record ReorderPayload(
    string VisitId,
    string UserId,
    string OccurrenceId,
    long ExpectedVersion,
    string? AnchorOccurrenceId,
    RideOccurrencePlacement Placement);
