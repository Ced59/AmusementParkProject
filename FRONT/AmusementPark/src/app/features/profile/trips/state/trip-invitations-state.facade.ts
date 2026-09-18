import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import {
  CreateTripInvitationRequest,
  TripInvitationCreation,
  TripInvitationList,
  TripInvitationRole,
  TripInvitationSummary
} from '@app/models/trips/trip-invitation.models';
import {
  TRIP_OPERATION_ID_PORT,
  TripOperationIdPort
} from './trip-state-data.ports';
import {
  TRIP_INVITATIONS_DATA_PORT,
  TripInvitationsDataPort
} from '@features/trips/state/trip-invitation-data.port';

export type TripInvitationActionStatus = 'idle' | 'loading' | 'creating' | 'revoking' | 'error';

@Injectable()
export class TripInvitationsStateFacade {
  private readonly invitationsSignal = signal<TripInvitationSummary[]>([]);
  private readonly creationSignal = signal<TripInvitationCreation | null>(null);
  private readonly inviterDisplayNameSignal = signal<string>('');
  private readonly statusSignal = signal<TripInvitationActionStatus>('idle');
  private readonly tripIdSignal = signal<string>('');
  private readonly planVersionSignal = signal<number>(0);
  private pendingCreateFingerprint: string | null = null;
  private pendingCreateIdempotencyKey: string | null = null;
  private listRequestGeneration = 0;

  readonly invitations: Signal<TripInvitationSummary[]> = this.invitationsSignal.asReadonly();
  readonly creation: Signal<TripInvitationCreation | null> = this.creationSignal.asReadonly();
  readonly inviterDisplayName: Signal<string> = this.inviterDisplayNameSignal.asReadonly();
  readonly status: Signal<TripInvitationActionStatus> = this.statusSignal.asReadonly();

  constructor(
    @Inject(TRIP_INVITATIONS_DATA_PORT) private readonly data: TripInvitationsDataPort,
    @Inject(TRIP_OPERATION_ID_PORT) private readonly operationIds: TripOperationIdPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(tripId: string, planVersion: number): void {
    if (!tripId || planVersion < 1) {
      return;
    }

    this.tripIdSignal.set(tripId);
    this.planVersionSignal.set(planVersion);
    this.listRequestGeneration++;
    if (this.statusSignal() === 'creating' || this.statusSignal() === 'revoking') {
      return;
    }

    const requestGeneration: number = this.listRequestGeneration;
    this.statusSignal.set('loading');
    this.data.list(tripId).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => {
        if (requestGeneration === this.listRequestGeneration && this.statusSignal() === 'loading') {
          this.statusSignal.set('idle');
        }
      })
    ).subscribe({
      next: (result: TripInvitationList): void => {
        if (requestGeneration !== this.listRequestGeneration) {
          return;
        }
        this.inviterDisplayNameSignal.set(result.inviterDisplayName);
        this.invitationsSignal.set(result.invitations);
        this.statusSignal.set('idle');
      },
      error: (): void => {
        if (requestGeneration === this.listRequestGeneration) {
          this.statusSignal.set('error');
        }
      }
    });
  }

  create(role: TripInvitationRole, lifetimeHours: number, targetEmail: string): void {
    const tripId: string = this.tripIdSignal();
    const expectedPlanVersion: number = this.planVersionSignal();
    if (!tripId || expectedPlanVersion < 1 || this.isBusy()) {
      return;
    }

    this.listRequestGeneration++;
    const request: CreateTripInvitationRequest = {
      expectedPlanVersion,
      proposedRole: role,
      lifetimeHours,
      targetEmail: targetEmail.trim() || null
    };
    const fingerprint: string = JSON.stringify({
      tripId,
      proposedRole: request.proposedRole,
      lifetimeHours: request.lifetimeHours,
      targetEmail: request.targetEmail
    });
    if (this.pendingCreateFingerprint !== fingerprint || !this.pendingCreateIdempotencyKey) {
      this.pendingCreateFingerprint = fingerprint;
      this.pendingCreateIdempotencyKey = this.operationIds.create();
    }
    this.creationSignal.set(null);
    this.statusSignal.set('creating');
    this.data.create(tripId, request, this.pendingCreateIdempotencyKey).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => {
        if (this.statusSignal() === 'creating') {
          this.statusSignal.set('idle');
        }
      })
    ).subscribe({
      next: (creation: TripInvitationCreation): void => {
        this.creationSignal.set(creation);
        this.pendingCreateFingerprint = null;
        this.pendingCreateIdempotencyKey = null;
        this.statusSignal.set('idle');
        this.refreshList();
      },
      error: (): void => this.statusSignal.set('error')
    });
  }

  revoke(invitation: TripInvitationSummary): void {
    const tripId: string = this.tripIdSignal();
    if (!tripId || this.isBusy()) {
      return;
    }

    this.listRequestGeneration++;
    this.statusSignal.set('revoking');
    this.data.revoke(
      tripId,
      invitation.invitationId,
      invitation.version,
      this.operationIds.create()
    ).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => {
        if (this.statusSignal() === 'revoking') {
          this.statusSignal.set('idle');
        }
      })
    ).subscribe({
      next: (): void => {
        this.invitationsSignal.update((items: TripInvitationSummary[]): TripInvitationSummary[] =>
          items.filter((item: TripInvitationSummary): boolean => item.invitationId !== invitation.invitationId));
        if (this.creationSignal()?.invitationId === invitation.invitationId) {
          this.creationSignal.set(null);
        }
        this.statusSignal.set('idle');
      },
      error: (): void => this.statusSignal.set('error')
    });
  }

  clearCreatedLink(): void {
    this.creationSignal.set(null);
  }

  isBusy(): boolean {
    return this.statusSignal() === 'loading'
      || this.statusSignal() === 'creating'
      || this.statusSignal() === 'revoking';
  }

  private refreshList(): void {
    const tripId: string = this.tripIdSignal();
    if (!tripId) {
      return;
    }
    const requestGeneration: number = ++this.listRequestGeneration;
    this.data.list(tripId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: TripInvitationList): void => {
        if (requestGeneration !== this.listRequestGeneration) {
          return;
        }
        this.inviterDisplayNameSignal.set(result.inviterDisplayName);
        this.invitationsSignal.set(result.invitations);
      }
    });
  }
}
