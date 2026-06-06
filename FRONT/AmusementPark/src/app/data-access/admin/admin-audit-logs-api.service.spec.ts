import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AdminAuditLogsApiService } from './admin-audit-logs-api.service';

describe('AdminAuditLogsApiService', () => {
  let service: AdminAuditLogsApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(AdminAuditLogsApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('builds search params with trimmed optional values', () => {
    service.search({
      page: 2,
      size: 25,
      actorEmail: ' admin@test.fr ',
      action: ' Update ',
      entityType: '',
      entityId: null,
      actorUserId: 'user-1',
      fromUtc: undefined,
      toUtc: '2026-01-01T00:00:00Z',
      traceId: ' trace '
    }).subscribe();

    const request = httpTestingController.expectOne((candidate) => candidate.url === `${environment.apiBaseUrl}admin/audit-logs`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('size')).toBe('25');
    expect(request.request.params.get('actorEmail')).toBe('admin@test.fr');
    expect(request.request.params.get('action')).toBe('Update');
    expect(request.request.params.get('actorUserId')).toBe('user-1');
    expect(request.request.params.get('toUtc')).toBe('2026-01-01T00:00:00Z');
    expect(request.request.params.get('traceId')).toBe('trace');
    expect(request.request.params.has('entityType')).toBeFalse();
    expect(request.request.params.has('entityId')).toBeFalse();
    request.flush({ data: [], pagination: { totalItems: 0, totalPages: 0, currentPage: 2, itemsPerPage: 25 } });
  });
});
