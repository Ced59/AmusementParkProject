import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';

import {
  TripDelegatedRole,
  TripParticipant,
  TripParticipantList
} from '@app/models/trips/trip-participant.models';
import {
  TRIP_PARTICIPANTS_DATA_PORT,
  TripParticipantsDataPort
} from './trip-state-data.ports';

export type TripParticipantsStatus = 'idle' | 'loading' | 'ready' | 'error';

@Injectable()
export class TripParticipantsStateFacade {
  private readonly participantsSignal = signal<TripParticipant[]>([]);
  private readonly statusSignal = signal<TripParticipantsStatus>('idle');
  private readonly busySignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private readonly canManageRolesSignal = signal<boolean>(false);
  private readonly canTransferOwnershipSignal = signal<boolean>(false);
  private readonly canLeaveSignal = signal<boolean>(false);
  private readonly leftSignal = signal<boolean>(false);
  private readonly tripMutationRevisionSignal = signal<number>(0);
  private tripPlanId: string = '';
  private tripVersion: number = 0;

  readonly participants: Signal<TripParticipant[]> = this.participantsSignal.asReadonly();
  readonly status: Signal<TripParticipantsStatus> = this.statusSignal.asReadonly();
  readonly busy: Signal<boolean> = this.busySignal.asReadonly();
  readonly error: Signal<boolean> = this.errorSignal.asReadonly();
  readonly canManageRoles: Signal<boolean> = this.canManageRolesSignal.asReadonly();
  readonly canTransferOwnership: Signal<boolean> = this.canTransferOwnershipSignal.asReadonly();
  readonly canLeave: Signal<boolean> = this.canLeaveSignal.asReadonly();
  readonly left: Signal<boolean> = this.leftSignal.asReadonly();
  readonly tripMutationRevision: Signal<number> = this.tripMutationRevisionSignal.asReadonly();

  constructor(
    @Inject(TRIP_PARTICIPANTS_DATA_PORT) private readonly data: TripParticipantsDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(tripPlanId: string): void {
    const normalizedId: string = tripPlanId.trim();
    if (!normalizedId) {
      return;
    }

    this.loadParticipants(normalizedId, true);
  }

  private loadParticipants(
    normalizedId: string,
    resetActionError: boolean,
    recoveredFromTripVersion: number | null = null
  ): void {
    this.tripPlanId = normalizedId;
    this.leftSignal.set(false);
    this.statusSignal.set('loading');
    if (resetActionError) {
      this.errorSignal.set(false);
    }
    this.data.list(normalizedId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: TripParticipantList): void => {
        this.apply(result);
        if (recoveredFromTripVersion !== null) {
          if (result.tripVersion > recoveredFromTripVersion) {
            this.signalTripMutation();
          }
          this.busySignal.set(false);
        }
        this.statusSignal.set('ready');
      },
      error: (): void => {
        if (recoveredFromTripVersion !== null) {
          this.busySignal.set(false);
        }
        this.statusSignal.set('error');
      }
    });
  }

  changeRole(memberId: string, role: TripDelegatedRole): void {
    this.run(this.data.changeRole(this.tripPlanId, memberId, role, this.tripVersion), (): void => {
      this.signalTripMutation();
    });
  }

  transferOwnership(memberId: string, previousOwnerRole: TripDelegatedRole): void {
    this.run(this.data.transferOwnership(
      this.tripPlanId,
      memberId,
      previousOwnerRole,
      this.tripVersion
    ), (): void => {
      this.signalTripMutation();
    });
  }

  leave(): void {
    if (this.busySignal() || !this.canLeaveSignal()) {
      return;
    }

    this.busySignal.set(true);
    this.errorSignal.set(false);
    this.data.leave(this.tripPlanId, this.tripVersion)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (): void => {
          this.markLeft();
        },
        error: (error: unknown): void => {
          if (this.isNotFound(error)) {
            this.markLeft();
            return;
          }
          this.reconcileLeave();
        }
      });
  }

  private run(operation: Observable<TripParticipantList>, onSuccess?: () => void): void {
    if (this.busySignal() || !this.tripPlanId) {
      return;
    }

    const attemptedTripVersion: number = this.tripVersion;
    this.busySignal.set(true);
    this.errorSignal.set(false);
    operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: TripParticipantList): void => {
        this.apply(result);
        onSuccess?.();
        this.busySignal.set(false);
      },
      error: (): void => {
        this.errorSignal.set(true);
        this.loadParticipants(this.tripPlanId, false, attemptedTripVersion);
      }
    });
  }

  private reconcileLeave(): void {
    this.errorSignal.set(true);
    this.data.list(this.tripPlanId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: TripParticipantList): void => {
        this.apply(result);
        this.statusSignal.set('ready');
        this.busySignal.set(false);
      },
      error: (error: unknown): void => {
        if (this.isNotFound(error)) {
          this.markLeft();
          return;
        }
        this.statusSignal.set('error');
        this.busySignal.set(false);
      }
    });
  }

  private markLeft(): void {
    this.participantsSignal.set([]);
    this.canManageRolesSignal.set(false);
    this.canTransferOwnershipSignal.set(false);
    this.canLeaveSignal.set(false);
    this.errorSignal.set(false);
    this.leftSignal.set(true);
    this.statusSignal.set('ready');
    this.busySignal.set(false);
  }

  private isNotFound(error: unknown): boolean {
    return error instanceof HttpErrorResponse && error.status === 404;
  }

  private apply(result: TripParticipantList): void {
    this.participantsSignal.set(result.participants);
    this.canManageRolesSignal.set(result.canManageRoles);
    this.canTransferOwnershipSignal.set(result.canTransferOwnership);
    this.canLeaveSignal.set(result.canLeave);
    this.tripVersion = result.tripVersion;
  }

  private signalTripMutation(): void {
    this.tripMutationRevisionSignal.update((revision: number): number => revision + 1);
  }
}
