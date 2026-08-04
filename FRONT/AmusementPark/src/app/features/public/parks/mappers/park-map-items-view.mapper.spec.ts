import { ParkMapItems } from '@app/models/parks/park-map-items';

import { mapParkMapItemsToViewModel } from './park-map-items-view.mapper';

describe('mapParkMapItemsToViewModel lifecycle actions', () => {
  it('disables directions when the parent park is not operating', () => {
    const response: ParkMapItems = {
      park: { id: 'park-1', name: 'Future Park', status: 'Planned', latitude: 48, longitude: 2 },
      items: [{ id: 'item-1', name: 'Future Ride', category: 'Attraction', type: 'RollerCoaster', latitude: 48.1, longitude: 2.1 }],
      zones: []
    };

    const viewModel = mapParkMapItemsToViewModel(response, 'en');

    expect(viewModel.isOpenToVisitors).toBe(false);
    expect(viewModel.markers[0].directionsActionEnabled).toBe(false);
    expect(viewModel.markers[0].detailActionRouteCommands).not.toBeNull();
  });
});
