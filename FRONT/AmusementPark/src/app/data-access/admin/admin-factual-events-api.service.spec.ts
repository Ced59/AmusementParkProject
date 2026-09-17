import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { AdminFactualEventsApiService } from './admin-factual-events-api.service';

describe('AdminFactualEventsApiService', (): void => {
  let service: AdminFactualEventsApiService;
  let httpTestingController: HttpTestingController;

  beforeEach((): void => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(AdminFactualEventsApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach((): void => httpTestingController.verify());

  it('searches the verification queue with explicit filters', (): void => {
    service.search({
      page: 2,
      size: 20,
      status: 'Draft',
      targetType: 'Park',
      eventType: 'OpeningCalendarChanged',
      confidence: 'High',
    }).subscribe((result): void => expect(result.pagination.currentPage).toBe(2));

    const request = httpTestingController.expectOne(
      (candidate): boolean => candidate.url === `${environment.apiBaseUrl}admin/factual-events`,
    );
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('status')).toBe('Draft');
    expect(request.request.params.get('targetType')).toBe('Park');
    expect(request.request.params.get('eventType')).toBe('OpeningCalendarChanged');
    expect(request.request.params.get('confidence')).toBe('High');
    request.flush({
      data: [],
      pagination: { totalItems: 0, totalPages: 0, currentPage: 2, itemsPerPage: 20 },
    });
  });

  it('verifies with the expected version and safely encodes the event id', (): void => {
    service.verify('event/1', { expectedVersion: 4 }).subscribe();

    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}admin/factual-events/event%2F1/verify`,
    );
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ expectedVersion: 4 });
    request.flush(null);
  });

  it('publishes with the expected version', (): void => {
    service.publish('event-1', { expectedVersion: 5 }).subscribe();

    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}admin/factual-events/event-1/publish`,
    );
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ expectedVersion: 5 });
    request.flush(null);
  });

  it('corrects and retracts through distinct audited endpoints', (): void => {
    service.correct('event-1', { expectedVersion: 6, supersedingEventId: 'event-2' }).subscribe();
    const correction = httpTestingController.expectOne(
      `${environment.apiBaseUrl}admin/factual-events/event-1/correct`,
    );
    expect(correction.request.body).toEqual({ expectedVersion: 6, supersedingEventId: 'event-2' });
    correction.flush(null);

    service.retract('event-1', { expectedVersion: 6, reasonCode: 'source-invalidated' }).subscribe();
    const retraction = httpTestingController.expectOne(
      `${environment.apiBaseUrl}admin/factual-events/event-1/retract`,
    );
    expect(retraction.request.body).toEqual({ expectedVersion: 6, reasonCode: 'source-invalidated' });
    retraction.flush(null);
  });
});
