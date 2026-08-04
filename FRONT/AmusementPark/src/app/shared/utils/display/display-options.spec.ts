import { PARK_STATUS_OPTIONS } from './display-options';

describe('PARK_STATUS_OPTIONS', () => {
  it('lists every lifecycle status in the requested admin order', () => {
    expect(PARK_STATUS_OPTIONS.map((option) => option.value)).toEqual([
      'Planned',
      'UnderConstruction',
      'Operating',
      'TemporarilyClosed',
      'ClosedDefinitively',
      'Cancelled',
    ]);
  });
});
