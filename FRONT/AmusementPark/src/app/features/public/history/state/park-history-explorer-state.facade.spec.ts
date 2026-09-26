import { TestBed } from '@angular/core/testing';

import {
  PublicHistoricalSubjectSnapshot,
  PublicHistoricalTimelineEntry,
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
      ]),
      createTimeline()
    );

    expect(facade.snapshotState().kind).toBe('ready');
    expect(facade.parkIdentity()?.displayName).toBe('Example Park');
    expect(facade.knownOpenSubjects().map((subject) => subject.displayName)).toEqual(['Coaster']);
    expect(facade.possiblyOpenSubjects().map((subject) => subject.displayName)).toEqual(['Old ride']);
    expect(facade.uncertainSubjects().map((subject) => subject.displayName)).toEqual(['Mystery ride']);
    expect(facade.zones().map((subject) => subject.displayName)).toEqual(['Western area']);
  });

  it('derives unique key years and only nearby events for the requested year', () => {
    facade.setResolvedSnapshot(createSnapshot([]), createTimeline());

    expect(facade.suggestedYears()).toEqual([1997, 1998, 2001]);
    expect(facade.nearbyEvents().map((event) => event.subjectLabel)).toEqual(['Opening', 'Rename']);
  });

  it('publishes an error state when no snapshot was resolved', () => {
    facade.setResolvedSnapshot(null, null);

    expect(facade.snapshotState().kind).toBe('error');
    expect(facade.timelineState().kind).toBe('empty');
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
    methodologyVersion: '1.0'
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
    events: [
      createEvent('Opening', 1997),
      createEvent('Rename', 1998),
      createEvent('Expansion', 2001),
      createEvent('Expansion duplicate year', 2001)
    ],
    pagination: { currentPage: 1, itemsPerPage: 50, totalItems: 4, totalPages: 1 }
  };
}

function createEvent(subjectLabel: string, year: number): PublicHistoricalTimelineEntry {
  return {
    subjectType: 'Park',
    subjectId: 'park-1',
    subjectLabel,
    factType: 'Other',
    period: { start: { year, precision: 'Year' }, startConfidence: 'Exact', endConfidence: 'Unknown' },
    evidenceState: 'Verified',
    importance: 'Standard',
    uncertaintyExplanations: [],
    sources: []
  };
}
