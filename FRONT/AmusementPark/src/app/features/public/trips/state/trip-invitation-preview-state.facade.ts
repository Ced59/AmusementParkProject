import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TripInvitationPreview } from '@app/models/trips/trip-invitation.models';
import {
  TRIP_INVITATIONS_DATA_PORT,
  TripInvitationsDataPort
} from '@features/trips/state/trip-invitation-data.port';

export type TripInvitationPreviewStatus = 'idle' | 'loading' | 'ready' | 'unavailable';

@Injectable()
export class TripInvitationPreviewStateFacade {
  private readonly previewSignal = signal<TripInvitationPreview | null>(null);
  private readonly statusSignal = signal<TripInvitationPreviewStatus>('idle');

  readonly preview: Signal<TripInvitationPreview | null> = this.previewSignal.asReadonly();
  readonly status: Signal<TripInvitationPreviewStatus> = this.statusSignal.asReadonly();

  constructor(
    @Inject(TRIP_INVITATIONS_DATA_PORT) private readonly data: TripInvitationsDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(token: string): void {
    const normalizedToken: string = token.trim();
    if (!normalizedToken || this.statusSignal() === 'loading') {
      this.statusSignal.set('unavailable');
      return;
    }

    this.previewSignal.set(null);
    this.statusSignal.set('loading');
    this.data.preview(normalizedToken).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (preview: TripInvitationPreview): void => {
        this.previewSignal.set(preview);
        this.statusSignal.set('ready');
      },
      error: (): void => this.statusSignal.set('unavailable')
    });
  }
}
