import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import {
  ProfileComparisonInvitationAcceptance,
  ProfileComparisonInvitationPreview
} from '@app/models/sharing/profile-comparison-invitation.models';
import {
  PROFILE_COMPARISON_INVITATION_PORT,
  ProfileComparisonInvitationPort
} from './profile-comparison-invitation-data.ports';

@Injectable()
export class ProfileComparisonInvitationAcceptanceStateFacade {
  private readonly previewSignal = signal<ProfileComparisonInvitationPreview | null>(null);
  private readonly acceptanceSignal = signal<ProfileComparisonInvitationAcceptance | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly acceptingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private token: string = '';

  public readonly preview: Signal<ProfileComparisonInvitationPreview | null> =
    this.previewSignal.asReadonly();
  public readonly acceptance: Signal<ProfileComparisonInvitationAcceptance | null> =
    this.acceptanceSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly accepting: Signal<boolean> = this.acceptingSignal.asReadonly();
  public readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(PROFILE_COMPARISON_INVITATION_PORT)
    private readonly port: ProfileComparisonInvitationPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  public load(token: string): void {
    this.token = token.trim();
    if (this.token.length === 0) {
      this.errorSignal.set(true);
      return;
    }

    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    this.port.preview(this.token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (preview: ProfileComparisonInvitationPreview): void => {
          this.previewSignal.set(preview);
          this.loadingSignal.set(false);
        },
        error: (): void => {
          this.loadingSignal.set(false);
          this.errorSignal.set(true);
        }
      });
  }

  public accept(): void {
    if (this.acceptingSignal() || !this.previewSignal()?.canAccept || this.token.length === 0) {
      return;
    }

    this.acceptingSignal.set(true);
    this.errorSignal.set(false);
    this.port.accept(this.token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (acceptance: ProfileComparisonInvitationAcceptance): void => {
          this.acceptanceSignal.set(acceptance);
          this.acceptingSignal.set(false);
        },
        error: (): void => {
          this.acceptingSignal.set(false);
          this.errorSignal.set(true);
        }
      });
  }
}
