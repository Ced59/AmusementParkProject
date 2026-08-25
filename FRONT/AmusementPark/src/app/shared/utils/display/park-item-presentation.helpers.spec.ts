import { getParkItemMarkerDetailTranslationKeys } from './park-item-presentation.helpers';

describe('getParkItemMarkerDetailTranslationKeys', () => {
  it('puts a public closed status before the item type', () => {
    expect(getParkItemMarkerDetailTranslationKeys('RollerCoaster', 'ClosedDefinitively')).toEqual([
      'parkItems.statuses.closedDefinitively',
      'parkExplorer.types.rollerCoaster',
    ]);
  });

  it('does not clutter operating markers with a status label', () => {
    expect(getParkItemMarkerDetailTranslationKeys('Restaurant', 'Operating')).toEqual([
      'parkExplorer.types.restaurant',
    ]);
  });
});
