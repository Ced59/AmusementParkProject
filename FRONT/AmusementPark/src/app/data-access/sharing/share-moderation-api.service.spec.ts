import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { ShareModerationApiService } from './share-moderation-api.service';

describe('ShareModerationApiService', (): void => {
  let service: ShareModerationApiService;
  let http: HttpTestingController;

  beforeEach((): void => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(ShareModerationApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach((): void => http.verify());

  it('submits the opaque public target and structured reason', (): void => {
    const body = { targetType: 'VisitRecap' as const, shareId: 'opaque-share',
      reason: 'PersonalData' as const, details: 'A phone number is visible.' };

    service.submit(body).subscribe();

    const request = http.expectOne(`${environment.apiBaseUrl}passport/shared/reports`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush(null);
  });

  it('maps the paged admin response and review action', (): void => {
    service.search({ page: 2, size: 20, status: 'Pending' }).subscribe((response): void => {
      expect(response.items[0].reportId).toBe('report-1');
      expect(response.pagination.currentPage).toBe(2);
    });
    const search = http.expectOne((request): boolean =>
      request.url === `${environment.apiBaseUrl}admin/share-moderation/reports`);
    expect(search.request.params.get('status')).toBe('Pending');
    search.flush({ data: [{ reportId: 'report-1' }], pagination: {
      totalItems: 1, totalPages: 1, currentPage: 2, itemsPerPage: 20,
    } });

    service.review('report 1', { decision: 'Suspend', note: 'Confirmed.' }).subscribe();
    const review = http.expectOne(
      `${environment.apiBaseUrl}admin/share-moderation/reports/report%201`,
    );
    expect(review.request.method).toBe('PUT');
    expect(review.request.body).toEqual({ decision: 'Suspend', note: 'Confirmed.' });
    review.flush(null);
  });
});
