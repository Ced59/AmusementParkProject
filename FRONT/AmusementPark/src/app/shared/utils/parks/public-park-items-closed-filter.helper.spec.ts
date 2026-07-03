import { Park } from '@app/models/parks/park';
import { DEFAULT_CLOSED_ENTITY_FILTER } from '@app/models/shared/closed-entity-filter';

import { resolvePublicParkItemsClosedFilter } from './public-park-items-closed-filter.helper';

describe('resolvePublicParkItemsClosedFilter', () => {
  it('uses all visible items by default for definitively closed parks', () => {
    const park: Pick<Park, 'status'> = { status: 'ClosedDefinitively' };

    expect(resolvePublicParkItemsClosedFilter(park, DEFAULT_CLOSED_ENTITY_FILTER)).toBe('all');
  });

  it('preserves explicit closed filters for definitively closed parks', () => {
    const park: Pick<Park, 'status'> = { status: 'ClosedDefinitively' };

    expect(resolvePublicParkItemsClosedFilter(park, 'all')).toBe('all');
    expect(resolvePublicParkItemsClosedFilter(park, 'closedOnly')).toBe('closedOnly');
  });

  it('keeps the requested filter for parks that are not definitively closed', () => {
    const park: Pick<Park, 'status'> = { status: 'Operating' };

    expect(resolvePublicParkItemsClosedFilter(park, DEFAULT_CLOSED_ENTITY_FILTER)).toBe('openOnly');
  });
});
