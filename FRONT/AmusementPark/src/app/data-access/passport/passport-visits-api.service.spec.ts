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
