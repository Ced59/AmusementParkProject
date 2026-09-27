import {
  PublicHistoricalCoverage,
  PublicHistoricalSubjectComparison,
  PublicParkHistoricalComparison
} from '@app/models/history/public-park-history.models';
import { ParkHistoryComparisonStateFacade } from './park-history-comparison-state.facade';

describe('ParkHistoryComparisonStateFacade', () => {
  it('separates lifecycle changes, transformations and stable presences', () => {
    const facade = new ParkHistoryComparisonStateFacade();
    const opened: PublicHistoricalSubjectComparison = createSubject('opened', 'Opened');
    const closed: PublicHistoricalSubjectComparison = createSubject('closed', 'Closed');
    const renamed: PublicHistoricalSubjectComparison = {
      ...createSubject('renamed', 'PresentAtBoth'),
      isRenamed: true,
      previousName: 'Ancien nom',
      nextName: 'Nouveau nom'
    };
    const stable: PublicHistoricalSubjectComparison = createSubject('stable', 'PresentAtBoth');
    const uncertain: PublicHistoricalSubjectComparison = createSubject('uncertain', 'Uncertain');

    facade.setResolvedComparison(createComparison([opened, closed, renamed, stable, uncertain]));

    expect(facade.opened()).toEqual([opened]);
    expect(facade.closed()).toEqual([closed]);
    expect(facade.changed()).toEqual([renamed]);
    expect(facade.stable()).toEqual([stable]);
    expect(facade.uncertain()).toEqual([uncertain]);
  });
});

function createSubject(
  comparisonKey: string,
  presenceChange: string
): PublicHistoricalSubjectComparison {
  return {
    comparisonKey,
    subjectType: 'ParkItem',
    displayName: comparisonKey,
    nameOrigin: 'Historical',
    presenceChange,
    isRenamed: false,
    isMoved: false,
    fromOperationalState: 'Unknown',
    toOperationalState: 'Unknown',
    fromSupportingSourceCount: 0,
    toSupportingSourceCount: 0
  };
}

function createComparison(
  subjects: PublicHistoricalSubjectComparison[]
): PublicParkHistoricalComparison {
  const coverage: PublicHistoricalCoverage = {
    totalSubjectCount: subjects.length,
    reliablePeriodSubjectCount: 0,
    partialPeriodSubjectCount: 0,
    undatedSubjectCount: subjects.length,
    name: { documentedSubjectCount: 0, applicableSubjectCount: 0, percentage: 0, isComplete: false },
    zone: { documentedSubjectCount: 0, applicableSubjectCount: 0, percentage: 0, isComplete: false },
    status: 'Partial'
  };
  return {
    parkId: 'park-1',
    parkName: 'Example Park',
    fromInstant: { year: 1998, precision: 'Year' },
    toInstant: { year: 2026, precision: 'Year' },
    subjects,
    categoryNetChanges: [],
    fromCoverage: coverage,
    toCoverage: coverage,
    fromUnclassifiedOpenItemCount: 0,
    toUnclassifiedOpenItemCount: 0,
    isCategoryComparisonComplete: true,
    methodologyVersion: 'hist-compare-v1'
  };
}
