import { PassportVisit } from '@app/models/passport/passport-visit.models';
import {
  canLogParkItemRide,
  formatParkItemRideReferenceDate,
  isParkItemRideRatingValid,
  mapParkItemRideDraftToRequest,
  mapPassportVisitToParkItemRideVisitOption
} from './park-item-passport-ride.mapper';

describe('park item passport ride mapper', () => {
  it('offers ride logging only for an identified attraction attached to a park', () => {
    const attraction = {
      id: 'ride-1',
      parkId: 'park-1',
      name: 'Le Grand Huit',
      category: 'Attraction' as const,
      type: 'RollerCoaster' as const,
      latitude: null,
      longitude: null
    };

    expect(canLogParkItemRide(attraction, null)).toBe(true);
    expect(canLogParkItemRide({ ...attraction, category: 'Restaurant' }, null)).toBe(false);
    expect(canLogParkItemRide({ ...attraction, id: undefined }, null)).toBe(false);
  });

  it('maps only draft visits and enables time only for an exact zoned day', () => {
    const visit: PassportVisit = createVisit();

    expect(mapPassportVisitToParkItemRideVisitOption(visit, 'fr')).toEqual({
      id: 'visit-1',
      dateLabel: '5 septembre 2026',
      title: 'Soirée',
      acceptsLocalTime: true
    });
    expect(mapPassportVisitToParkItemRideVisitOption({ ...visit, status: 'Completed' }, 'fr')).toBeNull();
    expect(mapPassportVisitToParkItemRideVisitOption({ ...visit, timeZoneId: null }, 'fr')?.acceptsLocalTime).toBe(false);
  });

  it('builds a bounded single-attraction batch without inventing a time', () => {
    expect(mapParkItemRideDraftToRequest('ride-1', {
      visitId: 'visit-1',
      count: 3,
      status: 'Completed',
      localTime: '14:35',
      isApproximate: true,
      rating: 4.5,
      confirmHistoricalConflict: true
    }, true)).toEqual({
      items: [{
        parkItemId: 'ride-1',
        moment: { localTime: '14:35:00', isApproximate: true },
        status: 'Completed',
        privateNote: null,
        confirmHistoricalConflict: true,
        count: 3
      }]
    });

    expect(mapParkItemRideDraftToRequest('ride-1', {
      visitId: 'visit-1',
      count: 1,
      status: 'Completed',
      localTime: '14:35',
      isApproximate: true,
      rating: null,
      confirmHistoricalConflict: false
    }, false)?.items[0].moment).toEqual({ localTime: null, isApproximate: false });
  });

  it('rejects invalid counts, times and rating steps', () => {
    const draft = {
      visitId: 'visit-1',
      count: 101,
      status: 'Completed' as const,
      localTime: '',
      isApproximate: false,
      rating: null,
      confirmHistoricalConflict: false
    };

    expect(mapParkItemRideDraftToRequest('ride-1', draft, false)).toBeNull();
    expect(mapParkItemRideDraftToRequest('ride-1', { ...draft, count: 1, localTime: '25:00' }, true)).toBeNull();
    expect(isParkItemRideRatingValid(4.5)).toBe(true);
    expect(isParkItemRideRatingValid(null)).toBe(true);
    expect(isParkItemRideRatingValid(4.2)).toBe(false);
    expect(formatParkItemRideReferenceDate('2025-04-03', 'fr')).toBe('3 avril 2025');
    expect(formatParkItemRideReferenceDate(null, 'fr')).toBe('—');
    expect(formatParkItemRideReferenceDate('2025-02-31', 'fr')).toBe('—');
  });
});

function createVisit(): PassportVisit {
  return {
    id: 'visit-1',
    parkId: 'park-1',
    parkName: 'Parc test',
    date: { year: 2026, month: 9, day: 5, precision: 'Day', isApproximate: false },
    timeZoneId: 'Europe/Paris',
    serviceDayConvention: 'VisitStartLocalDate',
    status: 'Draft',
    privacy: 'Private',
    title: 'Soirée',
    privateNote: null,
    version: 1,
    createdAtUtc: '2026-09-05T10:00:00Z',
    updatedAtUtc: '2026-09-05T10:00:00Z',
    completedAtUtc: null
  };
}
