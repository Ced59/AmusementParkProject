using System.Security.Cryptography;
using System.Text;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Visits;

public enum PassportAuditChangedField
{
    Visit = 1,
    Date = 2,
    Status = 3,
    ParkAssessmentRating = 4,
    ParkAssessmentPrivateComment = 5,
    RideOccurrence = 6,
    Moment = 7,
    HistoricalConsistency = 8,
    HistoricalTarget = 9,
    PrivateNote = 10,
    SortPosition = 11,
    DeletedAtUtc = 12,
    RideAssessmentRating = 13,
    RideAssessmentPrivateComment = 14,
    AssessmentRevision = 15,
    TimeZone = 16,
    ServiceDayConvention = 17,
    Title = 18,
}
