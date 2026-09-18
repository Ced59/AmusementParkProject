import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { of } from 'rxjs';

import { CreateTripInvitationRequest } from '@app/models/trips/trip-invitation.models';
import { environment } from '../../../environments/environment';
import { TripInvitationsApiService } from './trip-invitations-api.service';

describe('TripInvitationsApiService', () => {
  it('keeps owner invitation data out of SSR transfer caching', () => {
    const httpClient = { get: vi.fn().mockReturnValue(of([])) };
    const service: TripInvitationsApiService = new TripInvitationsApiService(httpClient as unknown as HttpClient);

    service.list('trip/one').subscribe();
    service.preview('opaque/token').subscribe();

    expect(httpClient.get.mock.calls).toEqual([
      [`${environment.apiBaseUrl}me/trips/trip%2Fone/invitations`, { transferCache: false }],
      [`${environment.apiBaseUrl}public/trip-invitations/opaque%2Ftoken/preview`, { transferCache: false }]
    ]);
  });

  it('creates and revokes links with idempotency and optimistic fences', () => {
    const httpClient = {
      post: vi.fn().mockReturnValue(of({})),
      delete: vi.fn().mockReturnValue(of(undefined))
    };
    const service: TripInvitationsApiService = new TripInvitationsApiService(httpClient as unknown as HttpClient);
    const request: CreateTripInvitationRequest = {
      expectedPlanVersion: 4,
      proposedRole: 'Participant',
      lifetimeHours: 168,
      targetEmail: null
    };

    service.create('trip-1', request, 'operation-create').subscribe();
    service.revoke('trip-1', 'invitation/one', 3, 'operation-revoke').subscribe();

    const createOptions = httpClient.post.mock.calls[0][2] as { headers: HttpHeaders };
    expect(createOptions.headers.get('Idempotency-Key')).toBe('operation-create');
    const revokeOptions = httpClient.delete.mock.calls[0][1] as { headers: HttpHeaders; params: HttpParams };
    expect(revokeOptions.headers.get('Idempotency-Key')).toBe('operation-revoke');
    expect(revokeOptions.params.get('expectedVersion')).toBe('3');
    expect(httpClient.delete.mock.calls[0][0]).toBe(
      `${environment.apiBaseUrl}me/trips/trip-1/invitations/invitation%2Fone`
    );
  });
});
