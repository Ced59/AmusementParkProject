import { inject, InjectionToken } from '@angular/core';

import { TripInvitationsApiService } from '@data-access/trips/trip-invitations-api.service';
import { PassportOperationIdService } from '@data-access/passport/passport-operation-id.service';

export interface TripInvitationsDataPort extends Pick<
  TripInvitationsApiService,
  'list' | 'create' | 'revoke' | 'preview' | 'accept' | 'decline'
> {
}

export const TRIP_INVITATIONS_DATA_PORT = new InjectionToken<TripInvitationsDataPort>(
  'TRIP_INVITATIONS_DATA_PORT',
  { providedIn: 'root', factory: (): TripInvitationsDataPort => inject(TripInvitationsApiService) }
);

export interface TripInvitationOperationIdPort extends Pick<PassportOperationIdService, 'create'> {
}

export const TRIP_INVITATION_OPERATION_ID_PORT = new InjectionToken<TripInvitationOperationIdPort>(
  'TRIP_INVITATION_OPERATION_ID_PORT',
  { providedIn: 'root', factory: (): TripInvitationOperationIdPort => inject(PassportOperationIdService) }
);
