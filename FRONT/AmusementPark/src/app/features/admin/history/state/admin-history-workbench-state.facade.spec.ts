import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';
import { vi } from 'vitest';

import {
  AdminHistoricalParkWorkbench,
  HistoricalEditorialMutation,
  HistoricalPublicationImpactPreview,
  SaveHistoricalFactRequest
} from '@app/models/history/admin-historical-workbench.models';
import {
  ADMIN_HISTORY_WORKBENCH_DATA_PORT,
  AdminHistoryWorkbenchDataPort
} from './admin-history-workbench-data.port';
import { AdminHistoryWorkbenchStateFacade } from './admin-history-workbench-state.facade';

describe('AdminHistoryWorkbenchStateFacade', () => {
  let facade: AdminHistoryWorkbenchStateFacade;
  let getWorkbench: ReturnType<typeof vi.fn>;
  let saveFact: ReturnType<typeof vi.fn>;
  let previewImpact: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    getWorkbench = vi.fn().mockReturnValue(of(createWorkbench('park-1')));
    saveFact = vi.fn().mockReturnValue(of(createMutation()));
    previewImpact = vi.fn().mockReturnValue(of(createPreview()));
    const dataPort: AdminHistoryWorkbenchDataPort = {
      getAdminParkWorkbench: getWorkbench as AdminHistoryWorkbenchDataPort['getAdminParkWorkbench'],
      previewAdminHistoricalImpact: previewImpact as AdminHistoryWorkbenchDataPort['previewAdminHistoricalImpact'],
      saveAdminHistoricalSource: vi.fn() as AdminHistoryWorkbenchDataPort['saveAdminHistoricalSource'],
      saveAdminHistoricalFact: saveFact as AdminHistoryWorkbenchDataPort['saveAdminHistoricalFact'],
      saveAdminHistoricalRelation: vi.fn() as AdminHistoryWorkbenchDataPort['saveAdminHistoricalRelation'],
      advanceAdminHistoricalResource: vi.fn() as AdminHistoryWorkbenchDataPort['advanceAdminHistoricalResource'],
      retractAdminHistoricalResource: vi.fn() as AdminHistoryWorkbenchDataPort['retractAdminHistoricalResource']
    };

    TestBed.configureTestingModule({
      providers: [
        AdminHistoryWorkbenchStateFacade,
        { provide: ADMIN_HISTORY_WORKBENCH_DATA_PORT, useValue: dataPort }
      ]
    });
    facade = TestBed.inject(AdminHistoryWorkbenchStateFacade);
  });

  it('keeps the latest selected park when an earlier request finishes later', () => {
    const first = new Subject<AdminHistoricalParkWorkbench>();
    const second = new Subject<AdminHistoricalParkWorkbench>();
    getWorkbench.mockImplementation((parkId: string): Observable<AdminHistoricalParkWorkbench> =>
      parkId === 'park-1' ? first : second
    );

    facade.load('park-1');
    facade.load('park-2');
    first.next(createWorkbench('park-1'));
    second.next(createWorkbench('park-2'));

    expect(facade.workbench()?.parkId).toBe('park-2');
    expect(facade.loading()).toBe(false);
  });

  it('reloads the canonical workbench after an immutable revision is saved', () => {
    const request = {} as SaveHistoricalFactRequest;
    facade.load('park-1');

    facade.saveFact('fact-1', request).subscribe();

    expect(saveFact).toHaveBeenCalledWith('park-1', 'fact-1', request);
    expect(getWorkbench).toHaveBeenCalledTimes(2);
    expect(facade.messageKey()).toBe('admin.history.workbench.messages.saved');
    expect(facade.busy()).toBe(false);
  });

  it('exposes a safe translated error when impact preview fails', () => {
    previewImpact.mockReturnValue(throwError(() => new Error('network')));
    facade.load('park-1');

    facade.previewImpact('Fact', 'fact-1', 1998);

    expect(previewImpact).toHaveBeenCalledWith('park-1', 'Fact', 'fact-1', 1998);
    expect(facade.preview()).toBeNull();
    expect(facade.errorKey()).toBe('admin.history.workbench.errors.previewFailed');
    expect(facade.busy()).toBe(false);
  });
});

function createWorkbench(parkId: string): AdminHistoricalParkWorkbench {
  return {
    parkId,
    parkName: parkId,
    subjects: [],
    facts: [],
    relations: [],
    sources: [],
    diagnostics: {
      parkId,
      parkName: parkId,
      factCount: 0,
      relationCount: 0,
      blockingIssueCount: 0,
      issues: [],
      decadeCoverage: [],
      workflow: [],
      visits: {
        potentiallyInconsistentVisitCount: 0,
        confirmedConflictVisitCount: 0,
        unverifiedVisitCount: 0
      },
      rolloutGate: {
        isOpen: false,
        hasEnoughStructuredFacts: false,
        hasCompleteSourceCoverage: false,
        hasMajorMilestone: false,
        hasIndexableKeyYear: false,
        publishedFactCount: 0,
        sourcedFactCount: 0,
        majorFactCount: 0,
        indexableKeyYears: []
      }
    }
  };
}

function createMutation(): HistoricalEditorialMutation {
  return {
    resourceType: 'Fact',
    resourceId: 'fact-1',
    revision: 2,
    workflowState: 'Draft',
    publicationState: 'Draft',
    factState: 'Unverified'
  };
}

function createPreview(): HistoricalPublicationImpactPreview {
  return {
    resourceType: 'Fact',
    resourceId: 'fact-1',
    resourceLabel: 'Opening',
    previewYear: 1998,
    canPublish: true,
    blockingReasons: [],
    affectedFromYear: 1998,
    affectedToYear: 1998,
    affectedSnapshotYearCount: 1,
    changedSubjectCount: 1,
    before: { knownOpenSubjectCount: 0, ambiguityCount: 0, reliablePeriodSubjectCount: 0, partialPeriodSubjectCount: 0, undatedSubjectCount: 0 },
    after: { knownOpenSubjectCount: 1, ambiguityCount: 0, reliablePeriodSubjectCount: 1, partialPeriodSubjectCount: 0, undatedSubjectCount: 0 },
    visits: { potentiallyInconsistentVisitCount: 0, confirmedConflictVisitCount: 0, unverifiedVisitCount: 0 }
  };
}
