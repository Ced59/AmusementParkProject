import { PassportRideOccurrence } from '@app/models/passport/passport-ride-occurrence.models';
import { ParkItem } from '@app/models/parks/park-item';
import {
  createAttractionSelection,
  mapAttractionSelectionToRequest,
  mapOccurrenceEditToRequest,
  mapOccurrenceToEditDraft,
  mapParkItemToVisitEditorAttraction,
  mapParkZoneToVisitEditorZone,
  normalizeCount,
  normalizeTimeForApi,
  normalizeTimeForInput
} from './passport-visit-editor.mapper';

describe('passport visit editor mapper', () => {
  it('maps attraction lifecycle and localized zone labels without accepting other categories', () => {
    const attraction: ParkItem = {
      id: 'ride-1',
      parkId: 'park-1',
      name: 'Old Ride',
      category: 'Attraction',
      type: 'RollerCoaster',
      latitude: null,
      longitude: null,
      attractionDetails: { status: 'Removed' }
    };

    expect(mapParkItemToVisitEditorAttraction(attraction)).toEqual(expect.objectContaining({
      id: 'ride-1',
      isHistorical: true,
      lifecycleStatus: 'Removed'
    }));
    expect(mapParkItemToVisitEditorAttraction({ ...attraction, category: 'Restaurant' })).toBeNull();
    expect(mapParkZoneToVisitEditorZone({
      id: 'zone-1',
      parkId: 'park-1',
      name: 'Fallback',
      names: [{ languageCode: 'fr', value: 'Le Village' }]
    }, 'fr')).toEqual({ id: 'zone-1', name: 'Le Village' });
  });

  it('normalizes count, optional text and API time while disabling time for imprecise visits', () => {
    const selection = createAttractionSelection({
      id: 'ride-1',
      name: 'Ride',
      zoneId: null,
      lifecycleStatus: 'Operating',
      isHistorical: false
    });
    const populated = {
      ...selection,
      count: 105,
      localTime: '09:42',
      isApproximate: true,
      privateNote: '  front row  '
    };

    expect(mapAttractionSelectionToRequest(populated, true)).toEqual(expect.objectContaining({
      count: 100,
      privateNote: 'front row',
      moment: { localTime: '09:42:00', isApproximate: true }
    }));
    expect(mapAttractionSelectionToRequest(populated, false).moment).toEqual({
      localTime: null,
      isApproximate: false
    });
    expect(normalizeCount(Number.NaN)).toBe(1);
    expect(normalizeTimeForApi('25:00')).toBeNull();
    expect(normalizeTimeForInput('09:42:30')).toBe('09:42');
  });

  it('round-trips an occurrence edit with its optimistic version and historical confirmation', () => {
    const occurrence: PassportRideOccurrence = createOccurrence();
    const draft = mapOccurrenceToEditDraft(occurrence);

    expect(draft).toEqual({
      status: 'Attempted',
      localTime: '14:05',
      isApproximate: true,
      privateNote: 'memory',
      confirmHistoricalConflict: true
    });
    expect(mapOccurrenceEditToRequest(occurrence, draft, true)).toEqual({
      expectedVersion: 4,
      moment: { localTime: '14:05:00', isApproximate: true },
      status: 'Attempted',
      privateNote: 'memory',
      confirmHistoricalConflict: true
    });
  });
});

function createOccurrence(): PassportRideOccurrence {
  return {
    id: 'occurrence-1',
    visitId: 'visit-1',
    parkId: 'park-1',
    parkItemId: 'ride-1',
    sortPosition: 1024,
    moment: { localTime: '14:05:00', isApproximate: true },
    status: 'Attempted',
    source: 'Manual',
    historicalConsistency: 'ConfirmedConflict',
    privateNote: 'memory',
    countsAsRide: false,
    version: 4,
    createdAtUtc: '2026-09-03T00:00:00Z',
    updatedAtUtc: '2026-09-03T00:00:00Z'
  };
}
