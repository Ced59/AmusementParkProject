import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';

import { mapParkItemToCardViewModel } from './park-item-card.mapper';

describe('mapParkItemToCardViewModel', () => {
  const park: Park = { id: 'park-1', name: 'Parc Test', latitude: 0, longitude: 0 };

  function createItem(overrides: Partial<ParkItem> = {}): ParkItem {
    return {
      id: 'item-1',
      parkId: 'park-1',
      name: ' Big Ride ',
      category: 'Attraction',
      type: 'RollerCoaster',
      latitude: 0,
      longitude: 0,
      descriptions: [{ languageCode: 'en', value: '<p>Description</p>' }],
      attractionDetails: {
        model: 'Hyper Coaster',
        status: 'Operating',
        heightInMeters: 50,
        speedInKmH: 120,
        inversionCount: 2
      } as never,
      ...overrides
    };
  }

  it('maps card identity, labels, icon and public item link', () => {
    const result = mapParkItemToCardViewModel(createItem(), park, 'fr', 'B&M', 'Zone A');

    expect(result.id).toBe('item-1');
    expect(result.name).toBe('Big Ride');
    expect(result.subtitle).toBe('B&M · Hyper Coaster');
    expect(result.categoryLabelKey).toBe('parkExplorer.categories.attraction');
    expect(result.typeLabelKey).toBe('parkExplorer.types.rollerCoaster');
    expect(result.typeIconClass).toBe('pi pi-bolt');
    expect(result.zoneName).toBe('Zone A');
    expect(result.imageUrl).toBeNull();
    expect(result.itemLink).toEqual(['/', 'fr', 'park', 'park-1', 'parc-test', 'item', 'item-1', 'big-ride']);
  });

  it('maps the optional card image urls', () => {
    const result = mapParkItemToCardViewModel(createItem(), park, 'fr', 'B&M', 'Zone A', null, 'Metric', undefined, '/images/main', '/images/main 640w');

    expect(result.imageUrl).toBe('/images/main');
    expect(result.imageSrcSet).toBe('/images/main 640w');
  });

  it('limits highlights to four values and localizes known statuses', () => {
    const result = mapParkItemToCardViewModel(createItem(), park, 'fr', 'B&M', null);

    expect(result.highlights).toEqual(['B&M', 'Hyper Coaster', 'En fonctionnement', '50 m']);
  });

  it('formats attraction highlights with imperial units when requested', () => {
    const result = mapParkItemToCardViewModel(
      createItem({
        attractionDetails: {
          heightInMeters: 60.96,
          speedInKmH: 120.7
        } as never
      }),
      park,
      'en',
      null,
      null,
      null,
      'Imperial'
    );

    expect(result.highlights).toEqual(['200 ft', '75 mph']);
  });

  it('falls back to raw status labels when status is unknown', () => {
    const result = mapParkItemToCardViewModel(createItem({ attractionDetails: { status: 'Soft opening' } as never }), park, 'en', null, null);

    expect(result.highlights).toEqual(['Soft opening']);
  });

  it('returns null subtitle and link when related data is missing', () => {
    const result = mapParkItemToCardViewModel(createItem({ id: undefined, attractionDetails: null }), null, 'en', null, null);

    expect(result.subtitle).toBeNull();
    expect(result.itemLink).toBeNull();
    expect(result.highlights).toEqual([]);
  });

  it('truncates card descriptions with the shared text truncator when provided', () => {
    const item: ParkItem = createItem({
      descriptions: [{ languageCode: 'en', value: 'A detailed attraction description '.repeat(12) }]
    });

    const result = mapParkItemToCardViewModel(item, park, 'en', null, null, new NaturalTextTruncatorService());

    expect(result.description?.length).toBeLessThanOrEqual(160);
    expect(result.description?.endsWith('...')).toBeTrue();
  });
});
