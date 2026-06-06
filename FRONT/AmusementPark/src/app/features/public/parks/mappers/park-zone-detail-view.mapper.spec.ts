import { ParkExplorerBucket } from '@app/models/parks/park-explorer';
import { ParkZone } from '@app/models/parks/park-zone';

import { mapParkZoneToDetailViewModel } from './park-zone-detail-view.mapper';

describe('mapParkZoneToDetailViewModel', () => {
  it('maps localized zone details, counts, coordinates and query params', () => {
    const zone: ParkZone = {
      id: 'zone-1',
      parkId: 'park-1',
      name: 'Fallback',
      names: [{ languageCode: 'fr', value: 'Zone FR' }],
      descriptions: [{ languageCode: 'fr', value: '<p>Description FR</p>' }],
      slug: 'zone-fr',
      latitude: 50,
      longitude: 3,
      isVisible: false,
      sortOrder: 2
    };
    const bucket: ParkExplorerBucket = {
      id: 'zone-1',
      name: 'Zone',
      isVirtual: false,
      totalItems: 10,
      countsByCategory: [
        { key: 'Attraction', count: 4 },
        { key: 'Shop', count: 0 }
      ],
      countsByType: [
        { key: 'RollerCoaster', count: 6 },
        { key: 'Restaurant', count: 3 },
        { key: 'Show', count: 2 },
        { key: 'Hotel', count: 1 }
      ]
    };

    const result = mapParkZoneToDetailViewModel(zone, bucket, ['/', 'fr'], 'fr');

    expect(result.name).toBe('Zone FR');
    expect(result.description).toBe('<p>Description FR</p>');
    expect(result.totalItems).toBe(10);
    expect(result.isVisible).toBeFalse();
    expect(result.queryParams).toEqual({ zone: 'zone-1' });
    expect(result.topCounts.map((count) => count.count)).toEqual([6, 4, 3, 2]);
    expect(result.infoRows.some((row) => row.value === '50, 3')).toBeTrue();
  });

  it('falls back to raw zone name, id and default visibility when localized values are missing', () => {
    expect(mapParkZoneToDetailViewModel({ id: 'z1', parkId: 'p1', name: ' Raw ', latitude: Number.NaN, longitude: Number.NaN }, null, null, 'en').name)
      .toBe('Raw');
    expect(mapParkZoneToDetailViewModel({ id: 'z2', parkId: 'p1' }, null, null, 'en').name).toBe('z2');
    expect(mapParkZoneToDetailViewModel({ parkId: 'p1' }, null, null, 'en').name).toBe('');
    expect(mapParkZoneToDetailViewModel({ parkId: 'p1' }, null, null, 'en').isVisible).toBeTrue();
  });
});
