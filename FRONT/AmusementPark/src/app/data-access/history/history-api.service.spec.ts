import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { HistoryApiService } from './history-api.service';

describe('HistoryApiService', () => {
  let service: HistoryApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(HistoryApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('leaves timeline retry orchestration to the shared HTTP policy', () => {
    const url: string = `${environment.apiBaseUrl}history/parks/park-1`;
    let receivedStatus: number | null = null;

    service.getParkTimeline('park-1').subscribe({
      error: (error: { status?: number }) => {
        receivedStatus = error.status ?? null;
      }
    });

    const request = httpTestingController.expectOne(url);
    expect(request.request.method).toBe('GET');
    request.flush(
      { errorCode: 'temporary.unavailable' },
      { status: 503, statusText: 'Service Unavailable' }
    );

    httpTestingController.expectNone(url);
    expect(receivedStatus).toBe(503);
  });

  it('loads a bounded canonical public timeline', () => {
    service.getPublicParkTimeline('park/one', {}, 2, 25).subscribe();

    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}public/parks/park%2Fone/history/timeline?page=2&pageSize=25`
    );
    expect(request.request.method).toBe('GET');
    request.flush({ parkId: 'park/one', parkName: 'Park', events: [], pagination: {} });
  });

  it('loads a public lineage with encoded subject coordinates', () => {
    service.getPublicHistoricalLineage('ParkItem', 'item/one').subscribe();

    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}public/history/subjects/ParkItem/item%2Fone/lineage`
    );
    expect(request.request.method).toBe('GET');
    request.flush({ root: {}, contextPark: null, subjects: [], relations: [], hasDirectedCycle: false, isTruncated: false, maximumDepth: 4 });
  });

  it('loads a canonical snapshot without inventing missing date precision', () => {
    service.getPublicParkSnapshot('park-1', 1998, 7).subscribe();

    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}public/parks/park-1/history/snapshot?year=1998&month=7`
    );
    expect(request.request.method).toBe('GET');
    request.flush({ parkId: 'park-1', parkName: 'Park', subjects: [], coverage: {}, ambiguities: [] });
  });
});
