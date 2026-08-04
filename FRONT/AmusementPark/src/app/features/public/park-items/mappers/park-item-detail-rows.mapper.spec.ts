import { ParkItem } from '@app/models/parks/park-item';

import { buildTechnicalRows } from './park-item-detail-rows.mapper';

describe('park item detail rows mapper', () => {
  it('uses translation keys for known imported technical values', () => {
    const item: ParkItem = {
      parkId: 'park-1',
      name: 'Mine Train Ulven',
      category: 'Attraction',
      type: 'RollerCoaster',
      latitude: null,
      longitude: null,
      attractionDetails: {
        materialType: 'Steel',
        seatingType: 'Sit Down',
        launchType: 'Lift à pneus',
        restraintType: 'Lap Bar commune'
      }
    };

    const valueKeys: Array<string | null | undefined> = buildTechnicalRows(item, null, 'de')
      .map((row) => row.valueKey);

    expect(valueKeys).toEqual([
      'parkItems.technicalValues.material.steel',
      'parkItems.technicalValues.seating.sitDown',
      'parkItems.technicalValues.launch.tireLift',
      'parkItems.technicalValues.restraint.sharedLapBar'
    ]);
  });
});
