import {
  buildPublicParkItemRouteCommands,
  buildPublicParkItemsRouteCommands,
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

  it('builds item route commands only when park and item identifiers are valid', () => {
    expect(buildPublicParkItemRouteCommands({ language: 'en', parkId: 'p1', parkName: 'Test Park', itemId: 'i1', itemName: 'Big Ride' }))
      .toEqual(['/', 'en', 'park', 'p1', 'test-park', 'item', 'i1', 'big-ride']);
    expect(buildPublicParkItemRouteCommands({ language: 'en', parkId: 'p1', parkName: 'Test Park', itemId: '', itemName: 'Big Ride' }))
      .toBeNull();
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
