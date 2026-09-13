import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';

import { SubmitShareModerationReportRequest } from '@app/models/sharing/share-moderation.models';
import { PUBLIC_SHARE_REPORT_PORT, PublicShareReportPort } from './public-share-report-state-data.ports';
import { PublicShareReportStateFacade } from './public-share-report-state.facade';

describe('PublicShareReportStateFacade', (): void => {
  let submitResponse: Observable<void>;
  let receivedRequest: SubmitShareModerationReportRequest | null;
  let facade: PublicShareReportStateFacade;

  beforeEach((): void => {
    submitResponse = of(void 0);
    receivedRequest = null;
    const port: PublicShareReportPort = {
      submit: (request: SubmitShareModerationReportRequest): Observable<void> => {
        receivedRequest = request;
        return submitResponse;
      },
    };
    TestBed.configureTestingModule({
      providers: [
        PublicShareReportStateFacade,
        { provide: PUBLIC_SHARE_REPORT_PORT, useValue: port },
      ],
    });
    facade = TestBed.inject(PublicShareReportStateFacade);
  });

  it('normalizes and submits a public report', (): void => {
    facade.submit('VisitRecap', ' token ', 'PersonalData', ' private details ');

    expect(receivedRequest).toEqual({
      targetType: 'VisitRecap',
      shareId: 'token',
      reason: 'PersonalData',
      details: 'private details',
    });
    expect(facade.submitted()).toBe(true);
    expect(facade.error()).toBe(false);
  });

  it('blocks duplicate submissions while the first request is pending', (): void => {
    const pending: Subject<void> = new Subject<void>();
    submitResponse = pending.asObservable();

    facade.submit('YearRecap', 'share', 'SpamOrUnsafeLink', 'details');
    facade.submit('YearRecap', 'share', 'SpamOrUnsafeLink', 'details');

    expect(facade.submitting()).toBe(true);
    pending.next();
    pending.complete();
    expect(facade.submitted()).toBe(true);
  });

  it('exposes an error without marking the report as submitted', (): void => {
    submitResponse = throwError((): Error => new Error('failed'));

    facade.submit('PassportProfile', 'share', 'Impersonation', 'details');

    expect(facade.error()).toBe(true);
    expect(facade.submitted()).toBe(false);
  });
});
