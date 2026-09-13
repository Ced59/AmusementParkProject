import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

import {
  ReviewShareModerationReportRequest,
  ShareModerationReport,
  ShareModerationReportQuery,
} from '@app/models/sharing/share-moderation.models';
import { PagedResult } from '@shared/models/contracts';
import {
  ADMIN_SHARE_MODERATION_STATE_PORT,
  AdminShareModerationStatePort,
} from './admin-share-moderation-state-data.ports';
import { AdminShareModerationStateFacade } from './admin-share-moderation-state.facade';

describe('AdminShareModerationStateFacade', (): void => {
  it('loads reports and refreshes the active filter after a decision', (): void => {
    const report: ShareModerationReport = {
      reportId: 'report-1', targetType: 'VisitRecap', reason: 'PersonalData',
      details: 'A phone number is visible.', status: 'Pending',
      submittedAtUtc: '2026-09-13T18:00:00Z', reviewedAtUtc: null, decisionNote: null,
    };
    const queries: ShareModerationReportQuery[] = [];
    let reviewRequest: ReviewShareModerationReportRequest | null = null;
    const port: AdminShareModerationStatePort = {
      search: (query: ShareModerationReportQuery): Observable<PagedResult<ShareModerationReport>> => {
        queries.push(query);
        return of({ items: [report], pagination: {
          totalItems: 1, totalPages: 1, currentPage: 1, itemsPerPage: 20,
        } });
      },
      review: (_: string, request: ReviewShareModerationReportRequest): Observable<void> => {
        reviewRequest = request;
        return of(void 0);
      },
    };
    TestBed.configureTestingModule({ providers: [
      AdminShareModerationStateFacade,
      { provide: ADMIN_SHARE_MODERATION_STATE_PORT, useValue: port },
    ] });
    const facade: AdminShareModerationStateFacade = TestBed.inject(AdminShareModerationStateFacade);
    const query: ShareModerationReportQuery = { page: 1, size: 20, status: 'Pending' };

    facade.load(query);
    facade.review('report-1', 'Suspend', ' Confirmed ');

    expect(facade.reports()).toEqual([report]);
    expect(reviewRequest).toEqual({ decision: 'Suspend', note: 'Confirmed' });
    expect(queries).toEqual([query, query]);
  });
});
