using System.Security.Cryptography;
using System.Text;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Visits;

public enum PassportAuditEntityType
{
    Visit = 1,
    ParkAssessment = 2,
    RideOccurrence = 3,
    RideAssessment = 4,
}
