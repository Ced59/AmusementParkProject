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
});
