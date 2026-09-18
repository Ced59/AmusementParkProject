import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';

import { TripInvitationPreview } from '@app/models/trips/trip-invitation.models';
import {
  TRIP_INVITATIONS_DATA_PORT,
  TripInvitationsDataPort
} from '@features/trips/state/trip-invitation-data.port';
import { TripInvitationPreviewStateFacade } from './trip-invitation-preview-state.facade';

describe('TripInvitationPreviewStateFacade', () => {
  let facade: TripInvitationPreviewStateFacade;
  let data: TripInvitationsDataPort;

  beforeEach(() => {
    data = {
      list: vi.fn(),
      create: vi.fn(),
      revoke: vi.fn(),
      preview: vi.fn().mockReturnValue(of(createPreview()))
    };
    TestBed.configureTestingModule({
      providers: [
        TripInvitationPreviewStateFacade,
        { provide: TRIP_INVITATIONS_DATA_PORT, useValue: data }
      ]
    });
    facade = TestBed.inject(TripInvitationPreviewStateFacade);
  });

  it('loads only the deliberately minimized public preview', () => {
    facade.load('opaque-token');

    expect(data.preview).toHaveBeenCalledWith('opaque-token');
    expect(facade.preview()).toEqual(createPreview());
    expect(facade.status()).toBe('ready');
    expect(Object.keys(facade.preview() ?? {})).not.toContain('tripPlanId');
  });

  it('uses one neutral unavailable state for every rejected token', () => {
    (data.preview as ReturnType<typeof vi.fn>).mockReturnValue(throwError(() => ({ status: 404 })));

    facade.load('unknown-token');

    expect(facade.preview()).toBeNull();
    expect(facade.status()).toBe('unavailable');
  });

  it('ignores an older preview response after the route token changes', () => {
    const firstPreview = new Subject<TripInvitationPreview>();
    const secondPreview: TripInvitationPreview = {
      ...createPreview(),
      tripTitle: 'Second voyage'
    };
    (data.preview as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce(firstPreview)
      .mockReturnValueOnce(of(secondPreview));

    facade.load('first-token');
    facade.load('second-token');
    firstPreview.next(createPreview());

    expect(facade.preview()).toEqual(secondPreview);
    expect(facade.status()).toBe('ready');
  });
});

function createPreview(): TripInvitationPreview {
  return {
    tripTitle: 'Voyage été',
    inviterDisplayName: 'Camille',
    proposedRole: 'Participant',
    periodKind: 'SingleMonth',
    startMonth: '2027-07',
    endMonth: '2027-07',
    memberCountBand: 'TwoToFive',
    expiresAtUtc: '2027-06-08T08:00:00Z',
    isTargeted: false
  };
}
