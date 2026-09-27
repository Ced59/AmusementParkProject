import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import type { MockedObject } from 'vitest';

import { PassportHistoricalExistenceReport } from '@app/models/passport/passport-historical-existence-report.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import {
  PASSPORT_HISTORICAL_EXISTENCE_REPORT_DATA_PORT,
  PassportHistoricalExistenceReportDataPort
} from './passport-historical-existence-report-data.port';
import { PassportHistoricalExistenceReportFacade } from './passport-historical-existence-report.facade';

describe('PassportHistoricalExistenceReportFacade', () => {
  let facade: PassportHistoricalExistenceReportFacade;
  let port: MockedObject<PassportHistoricalExistenceReportDataPort>;

  beforeEach(() => {
    port = {
      list: vi.fn().mockName('PassportHistoricalExistenceReportDataPort.list'),
      submit: vi.fn().mockName('PassportHistoricalExistenceReportDataPort.submit')
    } as unknown as MockedObject<PassportHistoricalExistenceReportDataPort>;
    TestBed.configureTestingModule({
      providers: [
        provideCommonTestDependencies(),
        PassportHistoricalExistenceReportFacade,
        { provide: PASSPORT_HISTORICAL_EXISTENCE_REPORT_DATA_PORT, useValue: port }
      ]
    });
    facade = TestBed.inject(PassportHistoricalExistenceReportFacade);
  });

  it('loads and prepends a newly submitted private report', () => {
    const previous: PassportHistoricalExistenceReport = createReport('previous');
    const created: PassportHistoricalExistenceReport = createReport('created');
    port.list.mockReturnValue(of([previous]));
    port.submit.mockReturnValue(of(created));

    facade.load('visit-1');
    facade.submit('visit-1', {
      claimedName: 'Ancien Cyclone',
      sourceUrl: null,
      sourceReference: null,
      details: null
    });

    expect(facade.reports()).toEqual([created, previous]);
    expect(facade.submissionStatus()).toBe('success');
  });

  it('distinguishes a duplicate pending report from rate limiting', () => {
    port.submit.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));

    facade.submit('visit-1', {
      claimedName: 'Ancien Cyclone',
      sourceUrl: null,
      sourceReference: null,
      details: null
    });

    expect(facade.submissionStatus()).toBe('conflict');
  });

  function createReport(reportId: string): PassportHistoricalExistenceReport {
    return {
      reportId,
      parkId: 'park-1',
      parkName: 'Parc témoin',
      visitDate: {
        year: 1998,
        month: null,
        day: null,
        precision: 'Year',
        isApproximate: true
      },
      claimedName: 'Ancien Cyclone',
      sourceUrl: null,
      sourceReference: null,
      details: null,
      status: 'Pending',
      submittedAtUtc: '2026-09-27T08:00:00Z',
      reviewedAtUtc: null,
      decisionNote: null,
      revision: 0
    };
  }
});
