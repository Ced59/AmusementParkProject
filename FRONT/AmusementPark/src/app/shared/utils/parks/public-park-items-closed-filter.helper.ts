import { Park } from '@app/models/parks/park';
import { ClosedEntityFilter, DEFAULT_CLOSED_ENTITY_FILTER } from '@app/models/shared/closed-entity-filter';

export function resolvePublicParkItemsClosedFilter(
  park: Pick<Park, 'status'> | null | undefined,
  requestedClosedFilter: ClosedEntityFilter = DEFAULT_CLOSED_ENTITY_FILTER
): ClosedEntityFilter {
  if (park?.status === 'ClosedDefinitively' && requestedClosedFilter === DEFAULT_CLOSED_ENTITY_FILTER) {
    return 'all';
  }

  return requestedClosedFilter;
}
