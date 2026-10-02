import { TestBed } from '@angular/core/testing';

import {
  PublicHistoricalSubjectSnapshot,
  PublicParkHistoricalSnapshot,
  PublicParkHistoricalTimeline
} from '@app/models/history/public-park-history.models';
import { ParkHistoryExplorerStateFacade } from './park-history-explorer-state.facade';

describe('ParkHistoryExplorerStateFacade', () => {
  let facade: ParkHistoryExplorerStateFacade;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [ParkHistoryExplorerStateFacade] });
    facade = TestBed.inject(ParkHistoryExplorerStateFacade);
  });

  it('groups snapshot subjects without treating uncertainty as certainty', () => {
    facade.setResolvedSnapshot(
      createSnapshot([
        createSubject('Park', 'KnownOpen', 'Example Park'),
        createSubject('ParkItem', 'KnownOpen', 'Coaster'),
        createSubject('ParkItem', 'PossiblyOpen', 'Old ride'),
        createSubject('ParkItem', 'Unknown', 'Mystery ride'),
        createSubject('ParkZone', 'KnownOpen', 'Western area')
      ])
    );

    expect(facade.snapshotState().kind).toBe('ready');
    expect(facade.parkIdentity()?.displayName).toBe('Example Park');
    expect(facade.knownOpenSubjects().map((subject) => subject.displayName)).toEqual(['Coaster']);
    expect(facade.possiblyOpenSubjects().map((subject) => subject.displayName)).toEqual(['Old ride']);
    expect(facade.uncertainSubjects().map((subject) => subject.displayName)).toEqual(['Mystery ride']);
    expect(facade.zones().map((subject) => subject.displayName)).toEqual(['Western area']);
  });

  it('publishes a resolved canonical timeline independently from a snapshot', () => {
    const timeline: PublicParkHistoricalTimeline = createTimeline();
    facade.setResolvedTimeline(timeline);

    expect(facade.timelineState().kind).toBe('ready');
    expect(facade.timeline()).toBe(timeline);
  });

  it('publishes an error state when no snapshot was resolved', () => {
    facade.setResolvedSnapshot(null);

    expect(facade.snapshotState().kind).toBe('error');
  });
});

function createSnapshot(subjects: PublicHistoricalSubjectSnapshot[]): PublicParkHistoricalSnapshot {
  return {
    parkId: 'park-1',
    parkName: 'Example Park',
    requestedInstant: { year: 1998, precision: 'Year' },
    subjects,
    coverage: {
      totalSubjectCount: subjects.length,
      reliablePeriodSubjectCount: subjects.length,
      partialPeriodSubjectCount: 0,
      undatedSubjectCount: 0,
      name: { documentedSubjectCount: subjects.length, applicableSubjectCount: subjects.length, percentage: 100, isComplete: true },
      zone: { documentedSubjectCount: 1, applicableSubjectCount: subjects.length, percentage: 20, isComplete: false },
      status: 'Substantial'
    },
    ambiguities: [],
    methodologyVersion: '1.0',
    isIndexableKeyYear: false
  };
}

function createSubject(subjectType: string, operationalState: string, displayName: string): PublicHistoricalSubjectSnapshot {
  return {
    subjectType,
    subjectId: displayName.toLowerCase().replace(/\s+/g, '-'),
    displayName,
    nameOrigin: 'Documented',
    operationalState,
    presenceExtent: 'WholePeriod',
    attributes: [],
    reasonCodes: [],
    supportingSourceCount: 1
  };
}

function createTimeline(): PublicParkHistoricalTimeline {
  return {
    parkId: 'park-1',
    parkName: 'Example Park',
    hasDecisionSnapshots: true,
    events: [],
    pagination: { currentPage: 1, itemsPerPage: 50, totalItems: 0, totalPages: 1 }
  };
}
