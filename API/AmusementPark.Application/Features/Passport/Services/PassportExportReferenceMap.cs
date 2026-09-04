using System.Globalization;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

internal sealed class PassportExportReferenceMap
{
    private readonly IReadOnlyDictionary<string, string> visitReferences;
    private readonly IReadOnlyDictionary<string, string> occurrenceReferences;
    private readonly IReadOnlyDictionary<string, string> parkReferences;
    private readonly IReadOnlyDictionary<string, string> parkItemReferences;

    private PassportExportReferenceMap(
        IReadOnlyDictionary<string, string> visitReferences,
        IReadOnlyDictionary<string, string> occurrenceReferences,
        IReadOnlyDictionary<string, string> parkReferences,
        IReadOnlyDictionary<string, string> parkItemReferences)
    {
        this.visitReferences = visitReferences;
        this.occurrenceReferences = occurrenceReferences;
        this.parkReferences = parkReferences;
        this.parkItemReferences = parkItemReferences;
    }

    public static PassportExportReferenceMap Create(PassportExportWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Dictionary<string, string> visits = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Visit visit in request.Visits)
        {
            AddReference(visits, visit.Id.Value, "visit");
        }

        foreach (RideOccurrence occurrence in request.RideOccurrences)
        {
            AddReference(visits, occurrence.VisitId.Value, "visit");
        }

        Dictionary<string, string> occurrences =
            new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (RideOccurrence occurrence in request.RideOccurrences)
        {
            AddReference(occurrences, occurrence.Id.Value, "occurrence");
        }

        Dictionary<string, string> parks = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Visit visit in request.Visits)
        {
            AddReference(parks, visit.ParkId, "park");
        }

        foreach (RideOccurrence occurrence in request.RideOccurrences)
        {
            AddReference(parks, occurrence.ParkId, "park");
        }

        Dictionary<string, string> parkItems =
            new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (RideOccurrence occurrence in request.RideOccurrences)
        {
            AddReference(parkItems, occurrence.ParkItemId, "park-item");
        }

        return new PassportExportReferenceMap(visits, occurrences, parks, parkItems);
    }

    public string Visit(VisitId visitId)
    {
        return this.visitReferences[visitId.Value];
    }

    public string Occurrence(RideOccurrenceId occurrenceId)
    {
        return this.occurrenceReferences[occurrenceId.Value];
    }

    public string Park(string parkId)
    {
        return this.parkReferences[parkId];
    }

    public string ParkItem(string parkItemId)
    {
        return this.parkItemReferences[parkItemId];
    }

    private static void AddReference(
        IDictionary<string, string> references,
        string internalId,
        string prefix)
    {
        if (references.ContainsKey(internalId))
        {
            return;
        }

        string index = (references.Count + 1).ToString("D4", CultureInfo.InvariantCulture);
        references.Add(internalId, $"{prefix}-{index}");
    }
}
