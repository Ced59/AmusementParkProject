import {
  buildPublicParkItemImagesRouteCommands,
  buildPublicParkItemRouteCommands,
  buildPublicParkItemsRouteCommands,
  buildPublicParkZoneRouteCommands,
  buildPublicParkZonesRouteCommands,
  buildPublicParkReferenceRouteCommands,
  buildPublicParkRouteCommands
} from './public-detail-route.helpers';

describe('public detail route helpers', () => {
  it('builds park route commands with localized language and slug', () => {
    expect(buildPublicParkRouteCommands({ language: 'fr', parkId: 'park-1', parkName: 'Parc Astérix' }))
      .toEqual(['/', 'fr', 'park', 'park-1', 'parc-asterix']);
  });

  it('returns null when required park route fields are missing', () => {
    expect(buildPublicParkRouteCommands({ language: 'fr', parkId: ' ', parkName: 'Parc' })).toBeNull();
    expect(buildPublicParkRouteCommands({ language: 'fr', parkId: 'id', parkName: null })).toBeNull();
  });

  it('extends park routes with the items segment', () => {
    expect(buildPublicParkItemsRouteCommands({ language: 'de', parkId: 'p1', parkName: 'Europa Park' }))
      .toEqual(['/', 'de', 'park', 'p1', 'europa-park', 'items']);
  });

  it('extends park routes with the zones segment', () => {
    expect(buildPublicParkZonesRouteCommands({ language: 'fr', parkId: 'p1', parkName: 'Parc Test' }))
      .toEqual(['/', 'fr', 'park', 'p1', 'parc-test', 'zones']);
  });

  it('builds zone route commands only when park and zone identifiers are valid', () => {
    expect(buildPublicParkZoneRouteCommands({ language: 'fr', parkId: 'p1', parkName: 'Parc Test', zoneId: 'z1', zoneName: 'Far West' }))
      .toEqual(['/', 'fr', 'park', 'p1', 'parc-test', 'zone', 'z1', 'far-west']);
    expect(buildPublicParkZoneRouteCommands({ language: 'fr', parkId: 'p1', parkName: 'Parc Test', zoneId: '', zoneName: 'Far West' }))
      .toBeNull();
  });

  it('builds item route commands only when park and item identifiers are valid', () => {
    expect(buildPublicParkItemRouteCommands({ language: 'en', parkId: 'p1', parkName: 'Test Park', itemId: 'i1', itemName: 'Big Ride' }))
      .toEqual(['/', 'en', 'park', 'p1', 'test-park', 'item', 'i1', 'big-ride']);
    expect(buildPublicParkItemRouteCommands({ language: 'en', parkId: 'p1', parkName: 'Test Park', itemId: '', itemName: 'Big Ride' }))
      .toBeNull();
  });

  it('extends item routes with the images segment', () => {
    expect(buildPublicParkItemImagesRouteCommands({ language: 'fr', parkId: 'p1', parkName: 'Parc Test', itemId: 'i1', itemName: 'Grand Huit' }))
      .toEqual(['/', 'fr', 'park', 'p1', 'parc-test', 'item', 'i1', 'grand-huit', 'images']);
  });

  it('builds reference routes for each supported reference kind', () => {
    expect(buildPublicParkReferenceRouteCommands({ language: 'pt', referenceId: 'op1', referenceName: 'Operator Inc', kind: 'operator' }))
      .toEqual(['/', 'pt', 'park-operator', 'op1', 'operator-inc']);
    expect(buildPublicParkReferenceRouteCommands({ language: 'pt', referenceId: 'mf1', referenceName: 'Intamin', kind: 'manufacturer' }))
      .toEqual(['/', 'pt', 'park-manufacturer', 'mf1', 'intamin']);
    expect(buildPublicParkReferenceRouteCommands({ language: 'pt', referenceId: 'fd1', referenceName: 'Founder', kind: 'founder' }))
      .toEqual(['/', 'pt', 'park-founder', 'fd1', 'founder']);
  });

  it('falls back to English for unsupported target languages', () => {
    expect(buildPublicParkRouteCommands({ language: 'xx', parkId: 'p1', parkName: 'Park' })?.[1]).toBe('en');
  });
});
