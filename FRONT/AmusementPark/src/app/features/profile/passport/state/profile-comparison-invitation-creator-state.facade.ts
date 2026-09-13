import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import {
  ProfileComparisonCategory,
  ProfileComparisonInvitationCreation
} from '@app/models/sharing/profile-comparison-invitation.models';
import {
  PROFILE_COMPARISON_INVITATION_PORT,
  ProfileComparisonInvitationPort
} from './profile-comparison-invitation-data.ports';

@Injectable()
export class ProfileComparisonInvitationCreatorStateFacade {
  private readonly selectedSignal = signal<ProfileComparisonCategory[]>([]);
  private readonly invitationSignal = signal<ProfileComparisonInvitationCreation | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);

  public readonly selected: Signal<ProfileComparisonCategory[]> = this.selectedSignal.asReadonly();
  public readonly invitation: Signal<ProfileComparisonInvitationCreation | null> =
    this.invitationSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(PROFILE_COMPARISON_INVITATION_PORT)
    private readonly port: ProfileComparisonInvitationPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  public initialize(categories: ProfileComparisonCategory[]): void {
    const available: Set<ProfileComparisonCategory> = new Set(categories);
    this.selectedSignal.set(this.selectedSignal().filter((category) => available.has(category)));
    if (this.selectedSignal().length === 0) {
      this.selectedSignal.set([...categories]);
    }
  }

  public toggle(category: ProfileComparisonCategory): void {
    const selected: ProfileComparisonCategory[] = this.selectedSignal();
    this.selectedSignal.set(selected.includes(category)
      ? selected.filter((value) => value !== category)
      : [...selected, category]);
    this.invitationSignal.set(null);
    this.errorSignal.set(false);
  }

  public create(): void {
    if (this.loadingSignal() || this.selectedSignal().length === 0) {
      return;
    }

    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    this.port.create(this.selectedSignal())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (invitation: ProfileComparisonInvitationCreation): void => {
          this.invitationSignal.set(invitation);
          this.loadingSignal.set(false);
        },
        error: (): void => {
          this.loadingSignal.set(false);
          this.errorSignal.set(true);
        }
      });
  }
}
