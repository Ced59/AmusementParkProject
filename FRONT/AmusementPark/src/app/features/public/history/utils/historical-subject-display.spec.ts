import { PublicHistoricalSubjectSnapshot } from '@app/models/history/public-park-history.models';
import { usesCurrentHistoricalSubjectNameFallback } from './historical-subject-display';

describe('historical subject display', () => {
  it('distinguishes a present-day fallback from a documented historical name', () => {
    expect(usesCurrentHistoricalSubjectNameFallback(createSubject('CurrentFallback'))).toBe(true);
    expect(usesCurrentHistoricalSubjectNameFallback(createSubject('Historical'))).toBe(false);
    expect(usesCurrentHistoricalSubjectNameFallback(createSubject('HistoricalLabel'))).toBe(false);
  });
});

function createSubject(nameOrigin: string): PublicHistoricalSubjectSnapshot {
  return {
    subjectType: 'ParkItem',
    subjectId: 'item-1',
    displayName: 'Nom visible',
    nameOrigin,
    operationalState: 'Unknown',
    presenceExtent: 'None',
    attributes: [],
    reasonCodes: [],
    supportingSourceCount: 0
  };
}
