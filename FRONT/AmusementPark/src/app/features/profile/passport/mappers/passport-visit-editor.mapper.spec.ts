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
      mainImageId: ' image-main-1 ',
      attractionDetails: { status: 'Removed' }
    };

    expect(mapParkItemToVisitEditorAttraction(attraction, {
      parkItemId: 'ride-1',
      historicalConsistency: 'ConfirmedConflict',
      openingDate: '1990-01-01',
      closingDate: '2010-12-31'
    })).toEqual(expect.objectContaining({
      id: 'ride-1',
      mainImageId: 'image-main-1',
      isHistorical: true,
      lifecycleStatus: 'Removed',
      historicalConsistency: 'ConfirmedConflict',
      openingDate: '1990-01-01',
      closingDate: '2010-12-31'
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
      mainImageId: null,
      zoneId: null,
      lifecycleStatus: 'Operating',
      isHistorical: false,
      historicalConsistency: 'Verified',
      openingDate: '2020-01-01',
      closingDate: null
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

  it('does not silently confirm a newly detected historical conflict', () => {
    const draft = mapOccurrenceToEditDraft({
      ...createOccurrence(),
      historicalConflictConfirmed: false
    });

    expect(draft.confirmHistoricalConflict).toBe(false);
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
    historicalConflictConfirmed: true,
    privateNote: 'memory',
    countsAsRide: false,
    version: 4,
    createdAtUtc: '2026-09-03T00:00:00Z',
    updatedAtUtc: '2026-09-03T00:00:00Z'
  };
}
