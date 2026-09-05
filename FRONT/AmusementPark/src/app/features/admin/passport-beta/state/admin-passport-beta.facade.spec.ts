import type { MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';

import {
  PassportBetaMetricsQuery,
  PassportBetaMetricsResult
} from '@app/models/passport/passport-beta-metrics.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import {
  ADMIN_PASSPORT_BETA_DATA_PORT,
  AdminPassportBetaDataPort
} from './admin-passport-beta-state-data.ports';
import { AdminPassportBetaFacade } from './admin-passport-beta.facade';

describe('AdminPassportBetaFacade', () => {
  let facade: AdminPassportBetaFacade;
  let port: MockedObject<AdminPassportBetaDataPort>;

  const metrics: PassportBetaMetricsResult = {
    generatedAtUtc: '2026-09-05T12:00:00.000Z',
    fromUtc: '2026-09-01T00:00:00.000Z',
    toUtc: '2026-09-05T23:59:59.999Z',
    createdVisits: 7,
    completedVisits: 5,
    usersWithCompletedVisit: 4,
    usersWithSecondCompletedVisit: 2,
    repeatUsageRatePercent: 50,
    repeatUsageSignal: 'Emerging',
    requiresQualitativeValidation: true,
    daily: [
      {
        date: '2026-09-05',
        completedVisits: 3,
        firstVisits: 1,
        secondVisits: 2
      }
    ]
  };

  beforeEach(() => {
    port = {
      getMetrics: vi.fn().mockName('AdminPassportBetaDataPort.getMetrics')
    } as unknown as MockedObject<AdminPassportBetaDataPort>;

    TestBed.configureTestingModule({
      providers: [
        provideCommonTestDependencies(),
        AdminPassportBetaFacade,
        { provide: ADMIN_PASSPORT_BETA_DATA_PORT, useValue: port }
      ]
    });
    facade = TestBed.inject(AdminPassportBetaFacade);
  });

  it('exposes the business return signal from aggregate data', () => {
    const query: PassportBetaMetricsQuery = {
      fromUtc: metrics.fromUtc,
      toUtc: metrics.toUtc
    };
    port.getMetrics.mockReturnValue(of(metrics));

    facade.load(query);

    expect(port.getMetrics).toHaveBeenCalledWith(query);
    expect(facade.state().kind).toBe('ready');
    expect(facade.usersWithSecondCompletedVisit()).toBe(2);
    expect(facade.repeatUsageRatePercent()).toBe(50);
    expect(facade.repeatUsageSignal()).toBe('Emerging');
    expect(facade.daily()).toEqual(metrics.daily);
  });

  it('keeps the preceding aggregates available when refresh fails', () => {
    port.getMetrics.mockReturnValue(of(metrics));
    facade.load();
    port.getMetrics.mockReturnValue(
      throwError(() => new Error('network')) as Observable<PassportBetaMetricsResult>
    );

    facade.load();

    expect(facade.state().kind).toBe('error');
    expect(facade.usersWithCompletedVisit()).toBe(4);
  });

  it('ignores a superseded response that completes after the latest request', () => {
    const firstResponse = new Subject<PassportBetaMetricsResult>();
    const latestResponse = new Subject<PassportBetaMetricsResult>();
    const latestMetrics: PassportBetaMetricsResult = {
      ...metrics,
      usersWithSecondCompletedVisit: 3,
      repeatUsageRatePercent: 75
    };
    port.getMetrics
      .mockReturnValueOnce(firstResponse)
      .mockReturnValueOnce(latestResponse);

    facade.load({ fromUtc: '2026-08-01T00:00:00.000Z' });
    facade.load({ fromUtc: '2026-09-01T00:00:00.000Z' });
    expect(firstResponse.observed).toBe(false);
    latestResponse.next(latestMetrics);
    firstResponse.next(metrics);

    expect(facade.state().kind).toBe('ready');
    expect(facade.usersWithSecondCompletedVisit()).toBe(3);
    expect(facade.repeatUsageRatePercent()).toBe(75);
  });
});
