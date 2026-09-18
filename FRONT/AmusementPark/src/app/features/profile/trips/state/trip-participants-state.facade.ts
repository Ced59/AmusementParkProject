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

    this.tripPlanId = normalizedId;
    this.leftSignal.set(false);
    this.statusSignal.set('loading');
    this.errorSignal.set(false);
    this.data.list(normalizedId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: TripParticipantList): void => {
        this.apply(result);
        this.statusSignal.set('ready');
      },
      error: (): void => this.statusSignal.set('error')
    });
  }

  changeRole(memberId: string, role: TripDelegatedRole): void {
    this.run(this.data.changeRole(this.tripPlanId, memberId, role, this.tripVersion));
  }

  transferOwnership(memberId: string, previousOwnerRole: TripDelegatedRole): void {
    this.run(this.data.transferOwnership(
      this.tripPlanId,
      memberId,
      previousOwnerRole,
      this.tripVersion
    ));
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
          this.participantsSignal.set([]);
          this.leftSignal.set(true);
          this.busySignal.set(false);
        },
        error: (): void => {
          this.errorSignal.set(true);
          this.busySignal.set(false);
        }
      });
  }

  private run(operation: Observable<TripParticipantList>): void {
    if (this.busySignal() || !this.tripPlanId) {
      return;
    }

    this.busySignal.set(true);
    this.errorSignal.set(false);
    operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: TripParticipantList): void => {
        this.apply(result);
        this.busySignal.set(false);
      },
      error: (): void => {
        this.errorSignal.set(true);
        this.busySignal.set(false);
        this.load(this.tripPlanId);
      }
    });
  }

  private apply(result: TripParticipantList): void {
    this.participantsSignal.set(result.participants);
    this.canManageRolesSignal.set(result.canManageRoles);
    this.canTransferOwnershipSignal.set(result.canTransferOwnership);
    this.canLeaveSignal.set(result.canLeave);
    this.tripVersion = result.tripVersion;
  }
}
