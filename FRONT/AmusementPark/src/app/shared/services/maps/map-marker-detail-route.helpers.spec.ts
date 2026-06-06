import { buildParkItemMapDetailRouteCommands, buildParkMapDetailRouteCommands } from './map-marker-detail-route.helpers';

describe('map-marker-detail-route helpers', () => {
  it('delegates park route command building to public route helpers', () => {
    expect(buildParkMapDetailRouteCommands({ language: 'fr', parkId: 'p1', parkName: 'Parc Test' }))
      .toEqual(['/', 'fr', 'park', 'p1', 'parc-test']);
  });

  it('delegates park item route command building to public route helpers', () => {
    expect(buildParkItemMapDetailRouteCommands({ language: 'fr', parkId: 'p1', parkName: 'Parc Test', itemId: 'i1', itemName: 'Ride Test' }))
      .toEqual(['/', 'fr', 'park', 'p1', 'parc-test', 'item', 'i1', 'ride-test']);
  });

  it('returns null when mandatory fields are missing', () => {
    expect(buildParkMapDetailRouteCommands({ language: 'fr', parkId: '', parkName: 'Parc Test' })).toBeNull();
    expect(buildParkItemMapDetailRouteCommands({ language: 'fr', parkId: 'p1', parkName: 'Parc Test', itemId: '', itemName: 'Ride Test' })).toBeNull();
  });
});
