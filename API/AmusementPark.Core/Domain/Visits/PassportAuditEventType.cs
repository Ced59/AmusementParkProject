using System.Security.Cryptography;
using System.Text;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Nature d'une preuve privée et immuable produite par une mutation du passeport.
/// </summary>
public enum PassportAuditEventType
{
    VisitCreated = 1,
    VisitDateChanged = 2,
    VisitCompleted = 3,
    VisitReopened = 4,
    VisitArchived = 5,
    VisitDeleted = 6,
    ParkAssessmentCreated = 7,
    ParkAssessmentChanged = 8,
    ParkAssessmentDeleted = 9,
    RideOccurrenceAdded = 10,
    RideOccurrenceChanged = 11,
    RideOccurrenceDeleted = 12,
    RideAssessmentCreated = 13,
    RideAssessmentChanged = 14,
    RideAssessmentDeleted = 15,
    VisitMetadataChanged = 16,
}
