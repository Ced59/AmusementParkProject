import type { MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';

import { ParkFitSourceReportRequest } from '@app/models/park-fit/park-fit-search.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import {
  PARK_FIT_SOURCE_REPORT_DATA_PORT,
  ParkFitSourceReportDataPort
} from './park-fit-source-report-data.port';
import { ParkFitSourceReportFacade } from './park-fit-source-report.facade';

describe('ParkFitSourceReportFacade', () => {
  let facade: ParkFitSourceReportFacade;
  let port: MockedObject<ParkFitSourceReportDataPort>;
  const request: ParkFitSourceReportRequest = {
    parkId: 'park-1',
    evidenceKind: 'AccessCondition',
    sourceUrl: 'https://example.org/access',
    sourceReference: null,
    reason: 'Outdated',
    details: null
  };

  beforeEach(() => {
    port = {
      submitReport: vi.fn().mockName('ParkFitSourceReportDataPort.submitReport')
    } as unknown as MockedObject<ParkFitSourceReportDataPort>;
    TestBed.configureTestingModule({
      providers: [
        provideCommonTestDependencies(),
        ParkFitSourceReportFacade,
        { provide: PARK_FIT_SOURCE_REPORT_DATA_PORT, useValue: port }
      ]
    });
    facade = TestBed.inject(ParkFitSourceReportFacade);
  });

  it('confirms a successfully submitted report', () => {
    port.submitReport.mockReturnValue(of(undefined));

    facade.submit(request);

    expect(port.submitReport).toHaveBeenCalledWith(request);
    expect(facade.status()).toBe('success');

    facade.submit(request);

    expect(port.submitReport).toHaveBeenCalledTimes(1);
  });

  it('exposes rate limiting without leaking a technical error', () => {
    port.submitReport.mockReturnValue(throwError(() =>
      new HttpErrorResponse({ status: 429 })));

    facade.submit(request);

    expect(facade.status()).toBe('rateLimited');
  });
});
