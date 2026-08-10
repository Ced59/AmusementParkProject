import { Park } from '@app/models/parks/park';

import { mapParkToDetailViewModel } from './park-detail-view.mapper';

describe('mapParkToDetailViewModel lifecycle actions', () => {
  const operatingPark: Park = {
    id: 'park-1',
    name: 'Example Park',
    status: 'Operating',
    latitude: 48,
    longitude: 2
  };

  it('exposes the item map only when a public item is geolocated', () => {
    expect(mapParkToDetailViewModel(operatingPark, 'en', {}, { mappableItemsCount: 0 }).mapLink).toBeNull();
    expect(mapParkToDetailViewModel(operatingPark, 'en', {}, { mappableItemsCount: 1 }).mapLink).not.toBeNull();
  });

  it('keeps visit planning actions for operating parks only', () => {
    const plannedPark: Park = { ...operatingPark, status: 'Planned' };

    expect(mapParkToDetailViewModel(operatingPark, 'en').weatherLink).not.toBeNull();
    expect(mapParkToDetailViewModel(operatingPark, 'en').openingHoursLink).not.toBeNull();
    expect(mapParkToDetailViewModel(
      operatingPark,
      'en',
      {},
      {},
      [],
      [],
      [],
      null,
      false,
      false,
      false,
      true
    ).pricingLink).toEqual([
      '/', 'en', 'park', 'park-1', 'example-park', 'pricing'
    ]);
    expect(mapParkToDetailViewModel(plannedPark, 'en').weatherLink).toBeNull();
    expect(mapParkToDetailViewModel(plannedPark, 'en').openingHoursLink).toBeNull();
    expect(mapParkToDetailViewModel(plannedPark, 'en').pricingLink).toBeNull();
  });

  it('does not expose pricing when the operating park has no current public offer', () => {
    expect(mapParkToDetailViewModel(operatingPark, 'en').pricingLink).toBeNull();
  });
});
