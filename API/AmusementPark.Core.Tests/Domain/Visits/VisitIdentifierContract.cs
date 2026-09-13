using System.Text.Json;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

internal sealed record VisitIdentifierContract(string VisitId, string RideOccurrenceId);
