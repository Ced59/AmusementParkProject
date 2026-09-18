import { inject, InjectionToken } from '@angular/core';

import { TripInvitationsApiService } from '@data-access/trips/trip-invitations-api.service';

export interface TripInvitationsDataPort extends Pick<
  TripInvitationsApiService,
  'list' | 'create' | 'revoke' | 'preview'
> {
}

export const TRIP_INVITATIONS_DATA_PORT = new InjectionToken<TripInvitationsDataPort>(
  'TRIP_INVITATIONS_DATA_PORT',
  { providedIn: 'root', factory: (): TripInvitationsDataPort => inject(TripInvitationsApiService) }
);
