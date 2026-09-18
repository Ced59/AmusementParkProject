import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TripInvitationPreview } from '@app/models/trips/trip-invitation.models';
import { AuthService } from '@app/services/auth/auth.service';
import {
  TRIP_INVITATIONS_DATA_PORT,
  TRIP_INVITATION_OPERATION_ID_PORT,
  TripInvitationOperationIdPort,
  TripInvitationsDataPort
} from '@features/trips/state/trip-invitation-data.port';

export type TripInvitationPreviewStatus =
  'idle' | 'loading' | 'ready' | 'unavailable' | 'deciding' | 'accepted' | 'declined' | 'decision-error';

@Injectable()
export class TripInvitationPreviewStateFacade {
  private readonly previewSignal = signal<TripInvitationPreview | null>(null);
  private readonly statusSignal = signal<TripInvitationPreviewStatus>('idle');
  private requestGeneration = 0;
  private token: string = '';
  private acceptOperationId: string | null = null;
  private declineOperationId: string | null = null;
  private readonly tripPlanIdSignal = signal<string | null>(null);

  readonly preview: Signal<TripInvitationPreview | null> = this.previewSignal.asReadonly();
  readonly status: Signal<TripInvitationPreviewStatus> = this.statusSignal.asReadonly();
  readonly tripPlanId: Signal<string | null> = this.tripPlanIdSignal.asReadonly();

  constructor(
    @Inject(TRIP_INVITATIONS_DATA_PORT) private readonly data: TripInvitationsDataPort,
    @Inject(TRIP_INVITATION_OPERATION_ID_PORT) private readonly operationIds: TripInvitationOperationIdPort,
    private readonly authService: AuthService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(token: string): void {
    const normalizedToken: string = token.trim();
    const tokenChanged: boolean = normalizedToken !== this.token;
    this.token = normalizedToken;
    const requestGeneration: number = ++this.requestGeneration;
    this.tripPlanIdSignal.set(null);
    if (tokenChanged) {
      this.acceptOperationId = null;
      this.declineOperationId = null;
    }
    if (!normalizedToken) {
      this.previewSignal.set(null);
      this.statusSignal.set('unavailable');
      return;
    }

    this.previewSignal.set(null);
    this.statusSignal.set('loading');
    this.data.preview(normalizedToken).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (preview: TripInvitationPreview): void => {
        if (requestGeneration !== this.requestGeneration) {
          return;
        }
        this.previewSignal.set(preview);
        this.statusSignal.set('ready');
      },
      error: (): void => {
        if (requestGeneration === this.requestGeneration) {
          this.previewSignal.set(null);
          this.statusSignal.set('unavailable');
        }
      }
    });
  }

  isAuthenticated(): boolean {
    return this.authService.isLoggedIn();
  }

  accept(): void {
    this.decide(true);
  }

  decline(): void {
    this.decide(false);
  }

  private decide(accept: boolean): void {
    if (!this.token || !this.isAuthenticated() || this.statusSignal() === 'deciding') {
      return;
    }

    const existingOperationId: string | null = accept
      ? this.acceptOperationId
      : this.declineOperationId;
    const operationId: string = existingOperationId ?? this.operationIds.create();
    if (accept) {
      this.acceptOperationId = operationId;
    } else {
      this.declineOperationId = operationId;
    }

    this.statusSignal.set('deciding');
    const decisionGeneration: number = this.requestGeneration;
    const decisionToken: string = this.token;
    const decision$ = accept
      ? this.data.accept(decisionToken, operationId)
      : this.data.decline(decisionToken, operationId);
    decision$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result): void => {
        if (decisionGeneration !== this.requestGeneration || decisionToken !== this.token) {
          return;
        }
        this.tripPlanIdSignal.set(result.tripPlanId);
        this.statusSignal.set(accept ? 'accepted' : 'declined');
      },
      error: (): void => {
        if (decisionGeneration === this.requestGeneration && decisionToken === this.token) {
          this.statusSignal.set('decision-error');
        }
      }
    });
  }
}
