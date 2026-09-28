import { AdminHistoricalSubject } from '@app/models/history/admin-historical-workbench.models';
import { filterHistoricalFactSubjectsForPark } from './admin-historical-workbench-subjects';

describe('filterHistoricalFactSubjectsForPark', () => {
  it('keeps local and legacy park subjects but excludes cross-park relation endpoints', () => {
    const localItem: AdminHistoricalSubject = subject('ParkItem', 'shared-item', 'park-1');
    const remoteItem: AdminHistoricalSubject = subject('ParkItem', 'shared-item', 'park-2');
    const legacyPark: AdminHistoricalSubject = subject('Park', 'park-1', null);

    expect(filterHistoricalFactSubjectsForPark(
      [remoteItem, localItem, legacyPark],
      ' park-1 '
    )).toEqual([localItem, legacyPark]);
  });
});

function subject(
  type: AdminHistoricalSubject['type'],
  id: string,
  contextParkId: string | null
): AdminHistoricalSubject {
  return {
    type,
    id,
    contextParkId,
    label: id,
    publicationPolicy: 'Public'
  };
}
