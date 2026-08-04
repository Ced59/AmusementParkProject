import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';

import { mapParkItemsZoneFocusViewModel } from './park-items-zone-focus.mapper';

describe('mapParkItemsZoneFocusViewModel lifecycle actions', () => {
  const item: ParkItem = {
    id: 'item-1',
    parkId: 'park-1',
    name: 'Future Ride',
    category: 'Attraction',
    type: 'RollerCoaster',
    latitude: 48.1,
    longitude: 2.1,
    isVisible: true,
  };

  it('disables directions on zone markers when the park is not operating', () => {
    const park: Park = {
      id: 'park-1',
      name: 'Future Park',
      status: 'Planned',
      latitude: 48,
      longitude: 2,
      isVisible: true,
    };

    const viewModel = mapParkItemsZoneFocusViewModel(
      park,
      null,
      [],
      [item],
      [item],
      null,
      [],
      'en',
    );

    expect(viewModel?.map.markers[0].directionsActionEnabled).toBe(false);
    expect(viewModel?.map.markers[0].detailActionRouteCommands).not.toBeNull();
  });

  it('keeps directions on zone markers for an operating park', () => {
    const park: Park = {
      id: 'park-1',
      name: 'Open Park',
      status: 'Operating',
      latitude: 48,
      longitude: 2,
      isVisible: true,
    };

    const viewModel = mapParkItemsZoneFocusViewModel(
      park,
      null,
      [],
      [item],
      [item],
      null,
      [],
      'en',
    );

    expect(viewModel?.map.markers[0].directionsActionEnabled).toBe(true);
  });
});
