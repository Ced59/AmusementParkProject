import type { MockedObject } from 'vitest';
import { HttpContext, HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, convertToParamMap } from '@angular/router';
import { Observable, firstValueFrom, of, throwError } from 'rxjs';

import { PublicHistoricalLineage } from '@app/models/history/public-park-history.models';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { HISTORY_DATA_PORT, HistoryDataPort } from './history-data.ports';
import { historicalLineageResolver } from './historical-lineage.resolver';

describe('historicalLineageResolver', () => {
  let historyDataPort: MockedObject<HistoryDataPort>;
  let ssrStatus: MockedObject<SsrHttpStatusService>;

  beforeEach(() => {
    historyDataPort = {
      getPublicHistoricalLineage: vi.fn().mockName('HistoryDataPort.getPublicHistoricalLineage')
    } as unknown as MockedObject<HistoryDataPort>;
    ssrStatus = {
      setNotFound: vi.fn().mockName('SsrHttpStatusService.setNotFound'),
      setStatus: vi.fn().mockName('SsrHttpStatusService.setStatus')
    } as unknown as MockedObject<SsrHttpStatusService>;
    TestBed.configureTestingModule({
      providers: [
        { provide: HISTORY_DATA_PORT, useValue: historyDataPort },
        { provide: SsrHttpStatusService, useValue: ssrStatus }
      ]
    });
  });

  it('loads a lineage anonymously before rendering the route', async () => {
    const lineage: PublicHistoricalLineage = createLineage();
    historyDataPort.getPublicHistoricalLineage.mockReturnValue(of(lineage));

    const result: PublicHistoricalLineage | null = await resolveLineage('ParkItem', 'item-1');

    expect(result).toBe(lineage);
    expect(historyDataPort.getPublicHistoricalLineage).toHaveBeenCalledWith(
      'ParkItem',
      'item-1',
      expect.objectContaining({ context: expect.any(HttpContext) })
    );
  });

  it('sets a not-found response when the lineage does not exist', async () => {
    historyDataPort.getPublicHistoricalLineage.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));

    const result: PublicHistoricalLineage | null = await resolveLineage('ParkItem', 'missing');

    expect(result).toBeNull();
    expect(ssrStatus.setNotFound).toHaveBeenCalledTimes(1);
  });

  function resolveLineage(subjectType: string, subjectId: string): Promise<PublicHistoricalLineage | null> {
    const route = {
      paramMap: convertToParamMap({ subjectType, subjectId })
    } as ActivatedRouteSnapshot;
    return TestBed.runInInjectionContext(() => firstValueFrom(
      historicalLineageResolver(route, {} as RouterStateSnapshot) as Observable<PublicHistoricalLineage | null>
    ));
  }
});

function createLineage(): PublicHistoricalLineage {
  return {
    root: { key: 'subject-1', type: 'ParkItem', label: 'Ancienne attraction', isHistoricalOnly: true },
    subjects: [
      { key: 'subject-1', type: 'ParkItem', label: 'Ancienne attraction', isHistoricalOnly: true },
      { key: 'subject-2', type: 'ParkItem', label: 'Nouvelle attraction', isHistoricalOnly: false }
    ],
    relations: [],
    hasDirectedCycle: false,
    isTruncated: false,
    maximumDepth: 4
  };
}
