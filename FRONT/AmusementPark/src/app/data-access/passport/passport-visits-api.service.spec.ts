import { HttpClient, HttpHeaders } from '@angular/common/http';
import { of } from 'rxjs';

import { CreatePassportVisitRequest } from '@app/models/passport/passport-visit.models';
import { environment } from '../../../environments/environment';
import { PassportVisitsApiService } from './passport-visits-api.service';

describe('PassportVisitsApiService', () => {
  it('sends the stable idempotency key on the private visit creation request', () => {
    const httpClient = {
      post: vi.fn().mockReturnValue(of({}))
    };
    const service: PassportVisitsApiService = new PassportVisitsApiService(httpClient as unknown as HttpClient);
    const request: CreatePassportVisitRequest = createRequest();

    service.createVisit(request, 'operation-123').subscribe();

    expect(httpClient.post).toHaveBeenCalledTimes(1);
    const call: unknown[] = httpClient.post.mock.calls[0];
    expect(call[0]).toBe(`${environment.apiBaseUrl}me/passport/visits`);
    expect(call[1]).toBe(request);
    const headers: HttpHeaders = (call[2] as { headers: HttpHeaders }).headers;
    expect(headers.get('Content-Type')).toBe('application/json');
    expect(headers.get('Idempotency-Key')).toBe('operation-123');
  });

  it('loads a private visit without allowing HTTP transfer caching', () => {
    const httpClient = {
      get: vi.fn().mockReturnValue(of({}))
    };
    const service: PassportVisitsApiService = new PassportVisitsApiService(httpClient as unknown as HttpClient);

    service.getVisit('visit/one').subscribe();

    expect(httpClient.get).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/passport/visits/visit%2Fone`,
      { transferCache: false }
    );
  });

  it('upserts a temporal park assessment through the visit-scoped endpoint', () => {
    const httpClient = {
      put: vi.fn().mockReturnValue(of({}))
    };
    const service: PassportVisitsApiService = new PassportVisitsApiService(httpClient as unknown as HttpClient);
    const request = { value: 4.5, privateComment: 'Belle journée', expectedVersion: 2 };

    service.upsertParkAssessment('visit/one', request).subscribe();

    expect(httpClient.put).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/passport/visits/visit%2Fone/assessment`,
      request
    );
  });

  it('updates visit metadata with the optimistic version in the payload', () => {
    const httpClient = { patch: vi.fn().mockReturnValue(of({})) };
    const service: PassportVisitsApiService = new PassportVisitsApiService(httpClient as unknown as HttpClient);
    const request = {
      date: { year: 2025, month: null, day: null, precision: 'Year' as const, isApproximate: true },
      timeZoneId: null,
      serviceDayConvention: 'VisitStartLocalDate' as const,
      title: 'Souvenir',
      privateNote: null,
      expectedVersion: 3
    };

    service.updateVisit('visit/one', request).subscribe();

    expect(httpClient.patch).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/passport/visits/visit%2Fone`,
      request
    );
  });

  it('posts every lifecycle transition with the optimistic version', () => {
    const httpClient = { post: vi.fn().mockReturnValue(of({})) };
    const service: PassportVisitsApiService = new PassportVisitsApiService(httpClient as unknown as HttpClient);

    service.completeVisit('visit/one', 2).subscribe();
    service.reopenVisit('visit/one', 3).subscribe();
    service.archiveVisit('visit/one', 4).subscribe();

    expect(httpClient.post.mock.calls).toEqual([
      [`${environment.apiBaseUrl}me/passport/visits/visit%2Fone/complete`, { expectedVersion: 2 }],
      [`${environment.apiBaseUrl}me/passport/visits/visit%2Fone/reopen`, { expectedVersion: 3 }],
      [`${environment.apiBaseUrl}me/passport/visits/visit%2Fone/archive`, { expectedVersion: 4 }]
    ]);
  });

  it('deletes a temporal park assessment with the parent version fence', () => {
    const httpClient = {
      delete: vi.fn().mockReturnValue(of({}))
    };
    const service: PassportVisitsApiService = new PassportVisitsApiService(httpClient as unknown as HttpClient);

    service.deleteParkAssessment('visit/one', 3).subscribe();

    expect(httpClient.delete).toHaveBeenCalledTimes(1);
    const call: unknown[] = httpClient.delete.mock.calls[0];
    expect(call[0]).toBe(`${environment.apiBaseUrl}me/passport/visits/visit%2Fone/assessment`);
    expect((call[1] as { params: { get: (name: string) => string | null } }).params.get('expectedVersion')).toBe('3');
  });
});

function createRequest(): CreatePassportVisitRequest {
  return {
    parkId: 'park-1',
    date: {
      year: 2026,
      month: 9,
      day: 3,
      precision: 'Day',
      isApproximate: false
    },
    timeZoneId: 'Europe/Paris',
    serviceDayConvention: 'VisitStartLocalDate',
    title: null,
    privateNote: null
  };
}
