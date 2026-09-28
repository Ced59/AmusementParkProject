import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { AdminHistoricalParkDiagnostics } from '@app/models/history/admin-historical-park-diagnostics.models';
import { Park } from '@app/models/parks/park';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import {
  ADMIN_HISTORY_DIAGNOSTICS_DATA_PORT,
  AdminHistoryDiagnosticsDataPort
} from './admin-history-diagnostics-data.port';
import {
  ADMIN_HISTORY_DIAGNOSTICS_PARKS_PORT,
  AdminHistoryDiagnosticsParksPort
} from './admin-history-diagnostics-parks.port';
import { AdminHistoryDiagnosticsStateFacade } from './admin-history-diagnostics-state.facade';

describe('AdminHistoryDiagnosticsStateFacade', () => {
  let facade: AdminHistoryDiagnosticsStateFacade;
  let getDiagnostics: ReturnType<typeof vi.fn>;
  let searchParks: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    getDiagnostics = vi.fn().mockReturnValue(of(createDiagnostics(2)));
    searchParks = vi.fn().mockReturnValue(of(createParkResponse()));
    const diagnosticsPort: AdminHistoryDiagnosticsDataPort = {
      getAdminParkDiagnostics: getDiagnostics as (
        parkId: string
      ) => Observable<AdminHistoricalParkDiagnostics>
    };
    const parksPort: AdminHistoryDiagnosticsParksPort = {
      searchParks: searchParks as AdminHistoryDiagnosticsParksPort['searchParks']
    };

    TestBed.configureTestingModule({
      providers: [
        AdminHistoryDiagnosticsStateFacade,
        { provide: ADMIN_HISTORY_DIAGNOSTICS_DATA_PORT, useValue: diagnosticsPort },
        { provide: ADMIN_HISTORY_DIAGNOSTICS_PARKS_PORT, useValue: parksPort }
      ]
    });
    facade = TestBed.inject(AdminHistoryDiagnosticsStateFacade);
  });

  it('searches parks by name without requiring a technical identifier', () => {
    facade.search('  Phantasialand  ');

    expect(searchParks).toHaveBeenCalledWith('Phantasialand', 1, 12, false);
    expect(facade.searchResults().map((park: Park) => park.name)).toEqual(['Phantasialand']);
    expect(facade.errorKey()).toBeNull();
  });

  it('loads the diagnostic and exposes its blocking state', () => {
    facade.load(' park-1 ');

    expect(getDiagnostics).toHaveBeenCalledWith('park-1');
    expect(facade.diagnostics()?.parkName).toBe('Phantasialand');
    expect(facade.hasBlockingIssues()).toBe(true);
    expect(facade.isRolloutOpen()).toBe(true);
    expect(facade.loading()).toBe(false);
  });

  it('clears stale diagnostics when loading fails', () => {
    facade.load('park-1');
    getDiagnostics.mockReturnValue(throwError(() => new Error('network')));

    facade.load('park-2');

    expect(facade.diagnostics()).toBeNull();
    expect(facade.errorKey()).toBe('admin.history.diagnostics.errors.loadFailed');
  });

  it('keeps the latest park selection when an earlier request finishes later', () => {
    const first = new Subject<AdminHistoricalParkDiagnostics>();
    const second = new Subject<AdminHistoricalParkDiagnostics>();
    getDiagnostics.mockImplementation((parkId: string): Observable<AdminHistoricalParkDiagnostics> =>
      parkId === 'park-1' ? first : second
    );

    facade.load('park-1');
    facade.load('park-2');
    first.next({ ...createDiagnostics(0), parkId: 'park-1', parkName: 'Premier parc' });
    second.next({ ...createDiagnostics(0), parkId: 'park-2', parkName: 'Second parc' });

    expect(facade.diagnostics()?.parkId).toBe('park-2');
    expect(facade.diagnostics()?.parkName).toBe('Second parc');
    expect(facade.loading()).toBe(false);
  });
});

function createDiagnostics(blockingIssueCount: number): AdminHistoricalParkDiagnostics {
  return {
    parkId: 'park-1',
    parkName: 'Phantasialand',
    factCount: 3,
    relationCount: 1,
    blockingIssueCount,
    issues: [],
    decadeCoverage: [],
    workflow: [],
    visits: {
      potentiallyInconsistentVisitCount: 2,
      confirmedConflictVisitCount: 1,
      unverifiedVisitCount: 1
    },
    rolloutGate: {
      isOpen: true,
      hasEnoughStructuredFacts: true,
      hasCompleteSourceCoverage: true,
      hasMajorMilestone: true,
      hasIndexableKeyYear: true,
      publishedFactCount: 2,
      sourcedFactCount: 2,
      majorFactCount: 1,
      indexableKeyYears: [1998]
    }
  };
}

function createParkResponse(): ParksApiResponse {
  return {
    data: [{
      id: 'park-1',
      name: 'Phantasialand',
      countryCode: 'DE',
      latitude: 50.8,
      longitude: 6.9
    }],
    pagination: {
      currentPage: 1,
      itemsPerPage: 12,
      totalItems: 1,
      totalPages: 1
    }
  };
}
