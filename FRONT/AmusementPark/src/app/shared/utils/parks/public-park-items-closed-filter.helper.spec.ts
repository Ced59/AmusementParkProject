import { Park } from '@app/models/parks/park';
import { DEFAULT_CLOSED_ENTITY_FILTER } from '@app/models/shared/closed-entity-filter';

import { resolvePublicParkItemsClosedFilter } from './public-park-items-closed-filter.helper';

describe('resolvePublicParkItemsClosedFilter', () => {
  it.each(['Planned', 'UnderConstruction', 'TemporarilyClosed', 'ClosedDefinitively', 'Cancelled'] as const)(
    'uses all visible items by default for %s parks',
    (status) => {
      const park: Pick<Park, 'status'> = { status };

      expect(resolvePublicParkItemsClosedFilter(park, DEFAULT_CLOSED_ENTITY_FILTER)).toBe('all');
    }
  );

  it('preserves explicit closed filters for non-operating parks', () => {
    const park: Pick<Park, 'status'> = { status: 'Planned' };

    expect(resolvePublicParkItemsClosedFilter(park, 'all')).toBe('all');
    expect(resolvePublicParkItemsClosedFilter(park, 'closedOnly')).toBe('closedOnly');
  });

  it('keeps the requested filter for operating parks', () => {
    const park: Pick<Park, 'status'> = { status: 'Operating' };

    expect(resolvePublicParkItemsClosedFilter(park, DEFAULT_CLOSED_ENTITY_FILTER)).toBe('openOnly');
  });
});
